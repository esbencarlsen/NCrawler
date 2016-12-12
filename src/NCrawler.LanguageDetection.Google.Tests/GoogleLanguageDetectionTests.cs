using System.Collections.Generic;

using NCrawler.HtmlProcessor;
using NCrawler.WebServer;

using NUnit.Framework;

namespace NCrawler.LanguageDetection.Google.Tests
{
	[TestFixture]
	public class GoogleLanguageDetectionTests
	{
		[Test]
		public void Test()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				List<PropertyBag> propertyBags = new List<PropertyBag>();
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.txt")
					.Do((crawler, bag) => { propertyBags.Add(bag); })
					.MaxCrawlCount(1)
					.Download()
					.TextProcessor()
					.DetectLanguage()
					.AddLoggerStep()
					.Run();
				Assert.Greater(propertyBags.Count, 1);
				string language = propertyBags[0][GoogleLanguageDetection.LanguagePropertyName].Value as string;
				Assert.IsNotNull(language);
				Assert.AreEqual("eng", language);
			}
		}
	}
}