using System;
using System.IO;

using NCrawler.Extensions;

namespace NCrawler.Utils
{
	/// <summary>
	/// 	Used for writing data to memory stream, but if data gets to large, writes dta to disk storage
	/// </summary>
	public class MemoryStreamWithFileBackingStore : Stream
	{
		#region Fields

		private MemoryStream _memoryStream = new MemoryStream();
		private long _bytesWritten;
		private FileStream _fileStoreStream;
		private readonly int _bufferSize;
		private TempFile _tempFile;
		private byte[] _data;

		#endregion

		#region Constructors

		public MemoryStreamWithFileBackingStore(int contentLength, long maxBytesInMemory, int bufferSize)
		{
			_bufferSize = bufferSize;
			if (contentLength > maxBytesInMemory)
			{
				_tempFile = new TempFile();
				_fileStoreStream = _fileStoreStream = new FileStream(_tempFile.FileName, FileMode.Create, FileAccess.Write, FileShare.Write, _bufferSize);
			}
			else
			{
				_memoryStream = new MemoryStream(contentLength < 0 ? _bufferSize : contentLength);
			}
		}

		#endregion

		#region Instance Properties

		public override bool CanRead
		{
			get { return false; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override long Length
		{
			get { throw new NotImplementedException(); }
		}

		public override long Position
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region Instance Methods

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_bytesWritten += count;
			if (_memoryStream != null)
			{
				_memoryStream.Write(buffer, offset, count);
			}
			else
			{
				_fileStoreStream.Write(buffer, offset, count);
			}
		}

		public void FinishedWriting()
		{
			if (_memoryStream != null)
			{
				_data = _memoryStream.ToArray();
				_memoryStream.Dispose();
				_memoryStream = null;
			}

			if (_fileStoreStream != null)
			{
				_fileStoreStream.Dispose();
				_fileStoreStream = null;
			}
		}

		public Stream GetReaderStream()
		{
			if (!_data.IsNull())
			{
				return new MemoryStream(_data);
			}

			return new FileStream(_tempFile.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _bufferSize);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				FinishedWriting();

				if (!_tempFile.IsNull())
				{
					_tempFile.Dispose();
					_tempFile = null;
				}
			}
		}

		#endregion
	}
}