using System.Collections.Generic;

using NCrawler.WebServer;

using NUnit.Framework;

namespace NCrawler.Robots.Tests
{
	[TestFixture]
	public class RobotsPipelineStepTests
	{
		[Test]
		public void CanHandleRobotsTxt()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				List<PropertyBag> propertyBags = new List<PropertyBag>();
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/NCrawler - Home.txt")
					.Do((crawler, bag) => { propertyBags.Add(bag); })
					.MaxCrawlCount(1)
					.Robots("/Content/Robots.txt")
					.Download()
					.AddLoggerStep()
					.Run();
				Assert.AreEqual(propertyBags.Count, 1);
				var allowed = (bool) propertyBags[0][RobotsPipelineStep.RobotsIsPathAllowedPropertyName].Value;
				Assert.AreEqual(false, allowed);
			}
		}
	}
}