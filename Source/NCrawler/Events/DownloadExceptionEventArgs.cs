using System;

namespace NCrawler.Events
{
	public class DownloadExceptionEventArgs : EventArgs
	{
		#region Constructors

		public DownloadExceptionEventArgs(CrawlStep crawlStep, CrawlStep referrrer, Exception exception)
		{
			CrawlStep = crawlStep;
			Referrer = referrrer;
			Exception = exception;
		}

		#endregion

		#region Instance Properties

		public CrawlStep CrawlStep { get; private set; }
		public CrawlStep Referrer { get; private set; }
		public Exception Exception { get; private set; }

		#endregion
	}
}