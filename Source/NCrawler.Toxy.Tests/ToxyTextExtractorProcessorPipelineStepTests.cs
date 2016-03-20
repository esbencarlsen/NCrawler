using NCrawler.WebServer;

using NUnit.Framework;

namespace NCrawler.Toxy.Tests
{
	[TestFixture]
	public class ToxyTextExtractorProcessorPipelineStepTests
	{
		[Test]
		public void ShouldExtractTextFromExcel()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				string extractedText = null;
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/SampleData.xls")
					.Download()
					.TextExtractProcessor()
					.AddLoggerStep()
					.Where((crawler, bag) =>
					{
						extractedText = bag.Text;
						return false;
					})
					.Run();

				Assert.IsNotNull(extractedText);
				var containsExpectedText = extractedText.Contains("Contextures Recommends");
				Assert.IsTrue(containsExpectedText, "Should contain expected text");
			}
		}

		[Test]
		public void ShouldExtractTextFromWord()
		{
			using (SimpleWebServer server = new SimpleWebServer())
			{
				string extractedText = null;
				new CrawlerConfiguration()
					.CrawlSeed(server.BaseUrl + "/Content/Georgia_opposition_NATO-Eng-F.doc")
					.Download()
					.TextExtractProcessor()
					.AddLoggerStep()
					.Where((crawler, bag) =>
					{
						extractedText = bag.Text;
						return false;
					})
					.Run();

				Assert.IsNotNull(extractedText);
				var containsExpectedText = extractedText.Contains("Georgian Parliamentary Faction");
				Assert.IsTrue(containsExpectedText, "Should contain expected text");
			}
		}
	}
}