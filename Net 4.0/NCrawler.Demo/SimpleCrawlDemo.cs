// --------------------------------------------------------------------------------------------------------------------- 
// <copyright file="SimpleCrawlDemo.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the SimpleCrawlDemo type.
// </summary>
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;

using NCrawler.Demo.Extensions;
using NCrawler.Extensions;
using NCrawler.HtmlProcessor;
using NCrawler.Interfaces;
using NCrawler.LanguageDetection.Google;

namespace NCrawler.Demo
{
	public class SimpleCrawlDemo
	{
		#region Class Methods

		public static void Run()
		{
			NCrawlerModule.Setup();
			Console.Out.WriteLine("Simple crawl demo");

			// Setup crawler to crawl http://ncrawler.codeplex.com
			// with 1 thread adhering to robot rules, and maximum depth
			// of 2 with 4 pipeline steps:
			//	* Step 1 - The Html Processor, parses and extracts links, text and more from html
			//  * Step 2 - Processes PDF files, extracting text
			//  * Step 3 - Try to determine language based on page, based on text extraction, using google language detection
			//  * Step 4 - Dump the information to the console, this is a custom step, see the DumperStep class
			using (Crawler c = new Crawler(new Uri("http://ncrawler.codeplex.com"),
				new HtmlDocumentProcessor(), // Process html
				new iTextSharpPdfProcessor.iTextSharpPdfProcessor(),
				new GoogleLanguageDetection(),
				new DumperStep())
				{
					// Custom step to visualize crawl
					MaximumThreadCount = 2,
					MaximumCrawlDepth = 10,
					ExcludeFilter = Program.ExtensionsToSkip,
				})
			{
				// Begin crawl
				c.Crawl();
			}
		}

		#endregion
	}

	#region Nested type: DumperStep

	/// <summary>
	/// 	Custom pipeline step, to dump url to console
	/// </summary>
	internal class DumperStep : IPipelineStep
	{
		#region IPipelineStep Members

		/// <summary>
		/// </summary>
		/// <param name = "crawler">
		/// 	The crawler.
		/// </param>
		/// <param name = "propertyBag">
		/// 	The property bag.
		/// </param>
		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			CultureInfo contentCulture = (CultureInfo) propertyBag["LanguageCulture"].Value;
			string cultureDisplayValue = "N/A";
			if (!contentCulture.IsNull())
			{
				cultureDisplayValue = contentCulture.DisplayName;
			}

			lock (this)
			{
				Console.Out.WriteLine(ConsoleColor.Gray, "Url: {0}", propertyBag.Step.Uri);
				Console.Out.WriteLine(ConsoleColor.DarkGreen, "\tContent type: {0}", propertyBag.ContentType);
				Console.Out.WriteLine(ConsoleColor.DarkGreen, "\tContent length: {0}",
					propertyBag.Text.IsNull() ? 0 : propertyBag.Text.Length);
				Console.Out.WriteLine(ConsoleColor.DarkGreen, "\tDepth: {0}", propertyBag.Step.Depth);
				Console.Out.WriteLine(ConsoleColor.DarkGreen, "\tCulture: {0}", cultureDisplayValue);
				Console.Out.WriteLine(ConsoleColor.DarkGreen, "\tThreadId: {0}", Thread.CurrentThread.ManagedThreadId);
				Console.Out.WriteLine(ConsoleColor.DarkGreen, "\tThread Count: {0}", crawler.ThreadsInUse);
				Console.Out.WriteLine();
			}
		}

		#endregion
	}

	#endregion
}