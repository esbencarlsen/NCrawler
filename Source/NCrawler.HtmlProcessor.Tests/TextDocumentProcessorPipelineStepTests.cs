using System.Collections.Generic;

using NCrawler.WebServer;

using NUnit.Framework;

namespace NCrawler.HtmlProcessor.Tests
{
	[TestFixture]
	public class TextDocumentProcessorPipelineStepTests
	{
		[Test]
		public void CanExtractTitleFromText()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				PropertyBag propertyBag = null;
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.txt")
					.Download()
					.MaxCrawlCount(1)
					.TextProcessor()
					.AddLoggerStep()
					.Do((crawler, bag) =>
					{
						propertyBag = bag;
					})
					.Run();

				Assert.IsNotNull(propertyBag);
				Assert.IsNotNull(propertyBag.Title);
				Assert.AreEqual(propertyBag.Step.Uri.ToString(), propertyBag.Title);
			}
		}

		[Test]
		public void CanExtractTextFromText()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				PropertyBag propertyBag = null;
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.txt")
					.MaxCrawlCount(1)
					.Download()
					.TextProcessor()
					.AddLoggerStep()
					.Do((crawler, bag) =>
					{
						propertyBag = bag;
					})
					.Run();

				Assert.IsNotNull(propertyBag);
				Assert.IsNotNull(propertyBag.Text);
				var containsExpectedText = propertyBag.Text.Contains("NCrawler - Home");
				Assert.IsTrue(containsExpectedText, "Should contain expected text");
			}
		}

		[Test]
		public void CanExtractLinksFromText()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				List<PropertyBag> collectedLinks = new List<PropertyBag>();
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.txt")
					.Do((crawler, bag) =>
					{
						collectedLinks.Add(bag);
					})
					.MaxCrawlCount(1)
					.Download()
					.TextProcessor()
					.AddLoggerStep()
					.Run();

				Assert.Greater(collectedLinks.Count, 1);
			}
		}
	}
}