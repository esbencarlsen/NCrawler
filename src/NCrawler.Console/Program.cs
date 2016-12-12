using System;

using NConsoler;

using NCrawler.Console.Extensions;
using NCrawler.HtmlProcessor;
using NCrawler.Robots;
using NCrawler.Utils;

namespace NCrawler.Console
{
	internal class Program
	{
		[Action]
		public static void Crawl([Required] string url,
			[Optional(1, AltNames = new[]{ "threads" }, Description = "Number of concurrent threads to use in crawl")] int threadCount,
			[Optional(false, AltNames = new[]{ "robotrules" }, Description = "Adhere to robot rules")] bool adhereToRobotRules,
			[Optional(0, Description = "Maximum crawl depth")] int depth,
			[Optional(20, AltNames = new[] { "connectiontimeout", "ct" }, Description = "Connection timeout in seconds(default 20)")] int connTimeout,
			[Optional(20, Description = "Read timeout in seconds(default 20)")] int timeout,
			[Optional(false, AltNames = new[] { "showtime", "sdt" }, Description = "Show download times")] bool showDownloadTimes,
			[Optional(0, Description = "Maximum number of downloads before stopping")] int maximumCrawlCount,
			[Optional(0, Description = "Maximum number of downloads errors before stopping")] int maximumHttpDownloadErrors,
			[Optional(0, Description = "Maximum crawl time in seconds before stopping")] int maximumCrawlTime,
			[Optional("NCrawl 3.0", Description = "User agent, default is NCrawl 3.0")] string userAgent)
		{
			AspectF.Define.
				Between("threads", threadCount, 1, 999).
				Between("depth", depth, 0, 999).
				Between("timeout", timeout, 0, 999).
				GreaterOrEqual("maximumCrawlCount", maximumCrawlCount, 0).
				GreaterOrEqual("maximumHttpDownloadErrors", maximumHttpDownloadErrors, 0).
				GreaterOrEqual("maximumCrawlTime", maximumCrawlTime, 0).
				Between("connectiontimeout", connTimeout, 0, 999);

			new CrawlerConfiguration()
				.CrawlSeed(url)
				.RemoveDuplicates()
				.WhereHostInCrawlSeed()
				.Robots()
				.Download(Environment.ProcessorCount * 3)
				.LogDownloadTime()
				.HtmlProcessor()
				.LogExceptions()
				.Run();
		}

		private static void Main(string[] args)
		{
			Consolery.Run(typeof (Program), args);
		}
	}
}