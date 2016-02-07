using System;
using System.Diagnostics;
using System.Net;

using NCrawler.Events;
using NCrawler.Services;
using NCrawler.Utils;

namespace NCrawler
{
	public class RequestState<T>
	{
		#region Instance Properties

		public CrawlStep CrawlStep { get; set; }
		public Exception Exception { get; set; }
		public DownloadMethod Method { get; set; }
		public PropertyBag PropertyBag { get; set; }
		public CrawlStep Referrer { get; set; }
		public Action<RequestState<T>> Complete { private get; set; }
		public Action<DownloadProgressEventArgs> DownloadProgress { get; set; }
		public Stopwatch DownloadTimer { get; set; }
		public HttpWebRequest Request { get; set; }
		public MemoryStreamWithFileBackingStore ResponseBuffer { get; set; }
		public int Retry { get; set; }
		public T State { get; set; }

		#endregion

		#region Instance Methods

		public void CallComplete(PropertyBag propertyBag, Exception exception)
		{
			Clean();

			PropertyBag = propertyBag;
			Exception = exception;
			Complete(this);
		}

		public void Clean()
		{
			if (ResponseBuffer != null)
			{
				ResponseBuffer.FinishedWriting();
				ResponseBuffer = null;
			}
		}

		#endregion
	}
}