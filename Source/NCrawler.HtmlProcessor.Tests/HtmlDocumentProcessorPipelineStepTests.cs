using System.Collections.Generic;
using System.Linq;

using NCrawler.WebServer;

using NUnit.Framework;

namespace NCrawler.HtmlProcessor.Tests
{
	[TestFixture]
	public class HtmlDocumentProcessorPipelineStepTests
	{
		[Test]
		public void CanExtractTitleFromHtml()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				PropertyBag propertyBag = null;
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.html")
					.Download()
					.MaxCrawlCount(1)
					.HtmlProcessor()
					.AddLoggerStep()
					.Do((crawler, bag) =>
					{
						propertyBag = bag;
					})
					.Run();

				Assert.IsNotNull(propertyBag);
				Assert.IsNotNull(propertyBag.Title);
				Assert.AreEqual("NCrawler - Home", propertyBag.Title);
			}
		}

		[Test]
		public void CanExtractMetaDataFromHtml()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				PropertyBag propertyBag = null;
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.html")
					.Download()
					.MaxCrawlCount(1)
					.HtmlProcessor()
					.AddLoggerStep()
					.Do((crawler, bag) =>
					{
						propertyBag = bag;
					})
					.Run();

				Assert.IsNotNull(propertyBag);
				Assert.IsNotNull(propertyBag["Meta"].Value);
				string[] metaData = (string[]) propertyBag["Meta"].Value;
				var containeMetaDataEntry = metaData.Any(x => x.Equals("twitter:title: NCrawler"));
				Assert.IsTrue(containeMetaDataEntry, "Should contain meta data");
			}
		}

		[Test]
		public void CanExtractTextFromHtml()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				PropertyBag propertyBag = null;
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.html")
					.MaxCrawlCount(1)
					.Download()
					.HtmlProcessor()
					.AddLoggerStep()
					.Do((crawler, bag) =>
					{
						propertyBag = bag;
					})
					.Run();

				Assert.IsNotNull(propertyBag);
				Assert.IsNotNull(propertyBag.Text);
				var containsExpectedText = propertyBag.Text.Contains("ncrawler - home");
				Assert.IsTrue(containsExpectedText, "Should contain expected text");
			}
		}

		[Test]
		public void CanExtractLinksFromHtml()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				List<PropertyBag> collectedLinks = new List<PropertyBag>();
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.html")
					.Do((crawler, bag) =>
					{
						collectedLinks.Add(bag);
					})
					.MaxCrawlCount(1)
					.Download()
					.HtmlProcessor()
					.AddLoggerStep()
					.Run();

				Assert.Greater(collectedLinks.Count, 1);
			}
		}
	}
}