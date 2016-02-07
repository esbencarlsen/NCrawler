using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace NCrawler.Extensions
{
	public static class StreamExtensions
	{
		#region Class Methods

		/// <summary>
		/// 	Copies any stream into a local MemoryStream
		/// </summary>
		/// <param name = "stream">The source stream.</param>
		/// <returns>The copied memory stream.</returns>
		public static MemoryStream CopyToMemory(this Stream stream)
		{
			MemoryStream memoryStream = new MemoryStream((int) stream.Length);
			stream.CopyToStream(memoryStream);
			return memoryStream;
		}

		public static void CopyToStream(this Stream source, Stream destination)
		{
#if !DOTNET4
			const int bufferSize = 1024*4;
			byte[] buffer = new byte[bufferSize];
			int bytesRead;
			while ((bytesRead = source.Read(buffer, 0, bufferSize)) > 0)
			{
				destination.Write(buffer, 0, bytesRead);
			}
#else
			source.CopyTo(destination);
#endif
		}

		public static void CopyToStreamAsync(this Stream source, Stream destination,
			Action<Stream, Stream, Exception> completed, Action<uint> progress,
			uint bufferSize, uint? maximumDownloadSize, TimeSpan? timeout)
		{
			byte[] buffer = new byte[bufferSize];

			Action<Exception> done = exception =>
				{
					if (completed != null)
					{
						completed(source, destination, exception);
					}
				};

			int maxDownloadSize = maximumDownloadSize.HasValue
				? (int)maximumDownloadSize.Value
				: int.MaxValue;
			int bytesDownloaded = 0;
			IAsyncResult asyncResult = source.BeginRead(buffer, 0, new[] {maxDownloadSize, buffer.Length}.Min(), null, null);
			Action<IAsyncResult, bool> endRead = null;
			endRead = (innerAsyncResult, innerIsTimedOut) =>
				{
					try
					{
						int bytesRead = source.EndRead(innerAsyncResult);
						if(innerIsTimedOut)
						{
							done(new TimeoutException());
						}

						int bytesToWrite = new[] { maxDownloadSize - bytesDownloaded, buffer.Length, bytesRead }.Min();
						destination.Write(buffer, 0, bytesToWrite);
						bytesDownloaded += bytesToWrite;

						if (!progress.IsNull() && bytesToWrite > 0)
						{
							progress((uint)bytesDownloaded);
						}

						if (bytesToWrite == bytesRead && bytesToWrite > 0)
						{
							asyncResult = source.BeginRead(buffer, 0, new[] { maxDownloadSize, buffer.Length }.Min(), null, null);
							// ReSharper disable PossibleNullReferenceException
							// ReSharper disable AccessToModifiedClosure
							asyncResult.FromAsync((ia, isTimeout) => endRead(ia, isTimeout), timeout);
							// ReSharper restore AccessToModifiedClosure
							// ReSharper restore PossibleNullReferenceException
						}
						else
						{
							done(null);
						}
					}
					catch (Exception exc)
					{
						done(exc);
					}
				};

			asyncResult.FromAsync((ia, isTimeout) => endRead(ia, isTimeout), timeout);
		}

		public static TResult FromBinary<TResult>(this Stream s) where TResult : class, new()
		{
			DataContractSerializer dc = new DataContractSerializer(typeof (TResult));
			return (TResult) dc.ReadObject(s);
		}

		/// <summary>
		/// 	Opens a StreamReader using the specified encoding.
		/// </summary>
		/// <param name = "stream">The stream.</param>
		/// <param name = "encoding">The encoding.</param>
		/// <returns>The stream reader</returns>
		public static StreamReader GetReader(this Stream stream, Encoding encoding)
		{
			if (!stream.CanRead)
			{
				throw new InvalidOperationException("Stream does not support reading.");
			}

			return encoding.IsNull()
				? new StreamReader(stream, true)
				: new StreamReader(stream, encoding);
		}

		/// <summary>
		/// 	Reads all text from the stream using the default encoding.
		/// </summary>
		/// <param name = "stream">The stream.</param>
		/// <returns>The result string.</returns>
		public static string ReadToEnd(this Stream stream)
		{
			return stream.ReadToEnd(null);
		}

		/// <summary>
		/// 	Reads all text from the stream using a specified encoding.
		/// </summary>
		/// <param name = "stream">The stream.</param>
		/// <param name = "encoding">The encoding.</param>
		/// <returns>The result string.</returns>
		public static string ReadToEnd(this Stream stream, Encoding encoding)
		{
			using (StreamReader reader = stream.GetReader(encoding))
			{
				return reader.ReadToEnd();
			}
		}

		#endregion
	}
}