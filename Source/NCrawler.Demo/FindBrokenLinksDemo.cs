using System;
using System.Net;

using NCrawler.HtmlProcessor;
using NCrawler.Interfaces;

namespace NCrawler.Demo
{
	/// <summary>
	/// 	Sample for binding broken links
	/// </summary>
	public class FindBrokenLinksDemo
	{
		#region Class Methods

		public static void Run()
		{
			NCrawlerModule.Setup();
			// Demo 2 - Find broken links
			Console.Out.WriteLine("\nFind broken links demo");

			// Setup crawler to crawl http://ncrawler.codeplex.com
			// with 2 thread adhering to robot rules, and maximum depth
			// of 2 with 2 pipeline steps
			NCrawlerModule.Setup();
			using (Crawler c = new Crawler(new Uri("http://ncrawler.codeplex.com"),
				new HtmlDocumentProcessor(), // Process html
				new DumpBrokenLinksStep()) // Custom pipeline Step
				{
					MaximumThreadCount = 5,
					MaximumCrawlDepth = 2,
				})
			{
				// Begin crawl
				c.Crawl();
			}
		}

		#endregion
	}

	#region Nested type: DumpBrokenLinksStep

	/// <summary>
	/// 	Custom pipeline step, to dump url to console if status code is not ok
	/// </summary>
	internal class DumpBrokenLinksStep : IPipelineStep
	{
		#region IPipelineStep Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK)
			{
				Console.Out.WriteLine("Url '{0}' referenced from {1} returned with statuscode {2}",
					propertyBag.Step.Uri, propertyBag.OriginalReferrerUrl, propertyBag.StatusCode);
				Console.Out.WriteLine();
			}
		}

		#endregion
	}

	#endregion
}