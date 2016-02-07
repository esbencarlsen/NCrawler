using System;

namespace NCrawler.Events
{
	public class DownloadProgressEventArgs : EventArgs
	{
		#region Instance Properties

		public uint BytesReceived { get; set; }

		public TimeSpan DownloadTime { get; set; }

		public double PercentCompleted
		{
			get
			{
				if (TotalBytesToReceive <= 0)
				{
					return 0;
				}

				return 100 - (100*(TotalBytesToReceive - BytesReceived))/TotalBytesToReceive;
			}
		}

		public CrawlStep Referrer { get; internal set; }
		public CrawlStep Step { get; internal set; }
		public uint TotalBytesToReceive { get; internal set; }

		#endregion
	}
}