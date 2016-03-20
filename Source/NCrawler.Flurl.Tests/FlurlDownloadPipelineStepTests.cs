using System.Collections.Generic;
using System.IO;
using System.Reflection;

using NCrawler.WebServer;

using NUnit.Framework;

namespace NCrawler.Flurl.Tests
{
	[TestFixture]
	public class FlurlDownloadPipelineStepTests
	{
		[Test]
		public void CanDownload()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				List<PropertyBag> downloads = new List<PropertyBag>();
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.html")
					.FlurlDownload()
					.MaxCrawlCount(1)
					.AddLoggerStep()
					.Do((crawler, bag) =>
					{
						downloads.Add(bag);
					})
					.Run();

				Assert.AreEqual(1, downloads.Count);
				Assert.IsNotNull(downloads[0].Response);
				Assert.GreaterOrEqual(downloads[0].Response.Length, new FileInfo(Path.Combine(
					new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName ?? string.Empty,
					"Content\\NCrawler - Home.html")).Length);
			}
		}

		[Test]
		public void CanDownloadWith()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				List<PropertyBag> downloads = new List<PropertyBag>();
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.html")
					.FlurlDownload()
					.MaxCrawlCount(1)
					.AddLoggerStep()
					.Do((crawler, bag) =>
					{
						downloads.Add(bag);
					})
					.Run();

				var flurlProperties = downloads[0][FlurlDownloadPipelineStep.FlurlHttpCallPropertyName].Value;
				Assert.IsNotNull(flurlProperties);
			}
		}
	}
}