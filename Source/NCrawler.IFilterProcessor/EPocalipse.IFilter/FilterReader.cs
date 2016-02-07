using System;
using System.IO;

namespace EPocalipse.IFilter
{
	/// <summary>
	/// 	Implements a TextReader that reads from an IFilter.
	/// </summary>
	public class FilterReader : TextReader
	{
		#region Readonly & Static Fields

		private readonly FilterWrapper _filter;

		#endregion

		#region Fields

		private char[] _charsLeftFromLastRead;
		private StatChunk _currentChunk;
		private bool _currentChunkValid;
		private bool _done;

		#endregion

		#region Constructors

		public FilterReader(string fileName)
		{
			_filter = FilterLoader.LoadAndInitIFilter(fileName);
			if (_filter == null)
			{
				throw new ArgumentException(string.Format("no filter defined for {0}", fileName));
			}
		}

		#endregion

		#region Instance Methods

		public override void Close()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public override int Read(char[] array, int offset, int count)
		{
			int endOfChunksCount = 0;
			int charsRead = 0;

			while (!_done && charsRead < count)
			{
				if (_charsLeftFromLastRead != null)
				{
					int charsToCopy = (_charsLeftFromLastRead.Length < count - charsRead)
						? _charsLeftFromLastRead.Length
						: count - charsRead;
					Array.Copy(_charsLeftFromLastRead, 0, array, offset + charsRead, charsToCopy);
					charsRead += charsToCopy;
					if (charsToCopy < _charsLeftFromLastRead.Length)
					{
						char[] tmp = new char[_charsLeftFromLastRead.Length - charsToCopy];
						Array.Copy(_charsLeftFromLastRead, charsToCopy, tmp, 0, tmp.Length);
						_charsLeftFromLastRead = tmp;
					}
					else
					{
						_charsLeftFromLastRead = null;
					}
					continue;
				}

				if (!_currentChunkValid)
				{
					FilterReturnCode res = _filter.Filter.GetChunk(out _currentChunk);
					_currentChunkValid = (res == FilterReturnCode.SOk) && ((_currentChunk.Flags & Chunkstate.ChunkText) != 0);

					if (res == FilterReturnCode.FilterEEndOfChunks)
					{
						endOfChunksCount++;
					}

					if (endOfChunksCount > 1)
					{
						_done = true; //That's it. no more chuncks available
					}
				}

				if (_currentChunkValid)
				{
					uint bufLength = (uint) (count - charsRead);
					if (bufLength < 8192)
					{
						bufLength = 8192; //Read ahead
					}

					char[] buffer = new char[bufLength];
					FilterReturnCode res = _filter.Filter.GetText(ref bufLength, buffer);
					if (res == FilterReturnCode.SOk || res == FilterReturnCode.FilterSLastText)
					{
						int cRead = (int) bufLength;
						if (cRead + charsRead > count)
						{
							int charsLeft = (cRead + charsRead - count);
							_charsLeftFromLastRead = new char[charsLeft];
							Array.Copy(buffer, cRead - charsLeft, _charsLeftFromLastRead, 0, charsLeft);
							cRead -= charsLeft;
						}
						else
						{
							_charsLeftFromLastRead = null;
						}

						Array.Copy(buffer, 0, array, offset + charsRead, cRead);
						charsRead += cRead;
					}

					if (res == FilterReturnCode.FilterSLastText || res == FilterReturnCode.FilterENoMoreText)
					{
						_currentChunkValid = false;
					}
				}
			}
			return charsRead;
		}

		protected override void Dispose(bool disposing)
		{
			_filter.Dispose();
		}

		#endregion
	}
}