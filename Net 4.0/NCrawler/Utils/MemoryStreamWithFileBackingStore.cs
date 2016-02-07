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

		private MemoryStream m_MemoryStream = new MemoryStream();
		private long bytesWritten;
		private FileStream m_FileStoreStream;
		private readonly int m_BufferSize;
		private TempFile m_TempFile;
		private byte[] m_Data;

		#endregion

		#region Constructors

		public MemoryStreamWithFileBackingStore(int contentLength, long maxBytesInMemory, int bufferSize)
		{
			m_BufferSize = bufferSize;
			if (contentLength > maxBytesInMemory)
			{
				m_TempFile = new TempFile();
				m_FileStoreStream = m_FileStoreStream = new FileStream(m_TempFile.FileName, FileMode.Create, FileAccess.Write, FileShare.Write, m_BufferSize);
			}
			else
			{
				m_MemoryStream = new MemoryStream(contentLength < 0 ? m_BufferSize : contentLength);
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
			bytesWritten += count;
			if (m_MemoryStream != null)
			{
				m_MemoryStream.Write(buffer, offset, count);
			}
			else
			{
				m_FileStoreStream.Write(buffer, offset, count);
			}
		}

		public void FinishedWriting()
		{
			if (m_MemoryStream != null)
			{
				m_Data = m_MemoryStream.ToArray();
				m_MemoryStream.Dispose();
				m_MemoryStream = null;
			}

			if (m_FileStoreStream != null)
			{
				m_FileStoreStream.Dispose();
				m_FileStoreStream = null;
			}
		}

		public Stream GetReaderStream()
		{
			if (!m_Data.IsNull())
			{
				return new MemoryStream(m_Data);
			}

			return new FileStream(m_TempFile.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, m_BufferSize);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				FinishedWriting();

				if (!m_TempFile.IsNull())
				{
					m_TempFile.Dispose();
					m_TempFile = null;
				}
			}
		}

		#endregion
	}
}