using System;
using System.Collections.Generic;

using NCrawler.Interfaces;

namespace NCrawler
{
	public partial class Crawler
	{
		#region Instance Properties

		/// <summary>
		/// Should the crawler follow the rules of the site beeing crawled.
		/// </summary>
		public bool AdhereToRobotRules { get; set; }

		public IEnumerable<IFilter> ExcludeFilter { get; set; }
		public IEnumerable<IFilter> IncludeFilter { get; set; }

		/// <summary>
		/// Maximum amount of time allowed to make a connection
		/// </summary>
		public TimeSpan? ConnectionReadTimeout { get; set; }

		/// <summary>
		/// In seconds
		/// </summary>
		public TimeSpan? ConnectionTimeout { get; set; }

		/// <summary>
		/// Maximum size a single download is allowed to be
		/// </summary>
		public uint? MaximumContentSize { get; set; }

		public uint DownloadBufferSize { get; set; }

		/// <summary>
		/// Maximum number of steps to download before ending crawl
		/// </summary>
		public int? MaximumCrawlCount { get; set; }

		/// <summary>
		/// Maximum levels to crawl into a website
		/// </summary>
		public int? MaximumCrawlDepth { get; set; }

		/// <summary>
		/// Maximum download error allowed before crawl is cancelled
		/// </summary>
		public int? MaximumHttpDownloadErrors { get; set; }

		/// <summary>
		/// Number of crawler threads to use
		/// </summary>
		public int MaximumThreadCount { get; set; }

		/// <summary>
		/// Maximum length of an url
		/// </summary>
		public int? MaximumUrlSize { get; set; }

		/// <summary>
		/// The maximum amount of time the crawler is allowed to run
		/// </summary>
		public TimeSpan? MaximumCrawlTime { get; set; }

		public IEnumerable<IPipelineStep> Pipeline { get; private set; }

		/// <summary>
		/// How many threads are currently in use
		/// </summary>
		public long ThreadsInUse
		{
			get { return m_ThreadInUse.Value; }
		}

		/// <summary>
		/// Determines how sensitive ncrawler is in following urls that contain upper and lower case characters
		/// </summary>
		public UriComponents UriSensitivity { get; set; }

		/// <summary>
		/// How the crawler should present itself to websites
		/// </summary>
		public string UserAgent { get; set; }

		/// <summary>
		/// How many times crawler should try to download a single url before giving up
		/// </summary>
		public int? DownloadRetryCount { get; set; }

		/// <summary>
		/// How long the crawler should wait before retrying a download
		/// </summary>
		public TimeSpan? DownloadRetryWaitDuration { get; set; }

		/// <summary>
		/// Use cookies when downloading
		/// </summary>
		public bool UseCookies { get; set; }

		/// <summary>
		/// How many url's are currently waiting to be downloaded/analysed
		/// </summary>
		public long WaitingQueueLength
		{
			get { return m_CrawlerQueue.Count; }
		}

		public uint MaximumDownloadSizeInRam { get; set; }

		#endregion
	}
}