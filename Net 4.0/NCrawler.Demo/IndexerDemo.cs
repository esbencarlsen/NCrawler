using System;
using System.Collections.Generic;

using NCrawler.HtmlProcessor;
using NCrawler.Interfaces;

namespace NCrawler.Demo
{
	public class IndexerDemo : IPipelineStep
	{
		#region IPipelineStep Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			string textContent = propertyBag.Text; // Filtered text content

			// Here you can send downloaded filtered content to an index, database, filesystem or whatever
			Console.Out.WriteLine(textContent);
		}

		#endregion

		#region Class Methods

		public static void Run()
		{
			NCrawlerModule.Setup();
			Console.Out.WriteLine("\nSimple indexer demo");

			// Setup crawler to crawl/index http://ncrawler.codeplex.com
			// 	* Step 1 - The Html Processor, parses and extracts links, text and more from html
			//  * Step 2 - Custom step, that is supposed to send content to an Index or Database
			using (Crawler c = new Crawler(new Uri("http://ncrawler.codeplex.com"),
				new HtmlDocumentProcessor( // Process html, filter links and content
				// Setup filter that removed all the text between <body and </body>
				// This can be custom tags like <!--BeginTextFiler--> and <!--EndTextFiler-->
				// or whatever you prefer. This way you can control what text is extracted on every page
				// Most cases you want just to filter the header information or menu text
					new Dictionary<string, string>
						{
							{"<body", "</body>"}
						},
				// Setup filter that tells the crawler not to follow links between tags
				// that start with <head and ends with </head>. This can be custom tags like
				// <!--BeginNoFollow--> and <!--EndNoFollow--> or whatever you prefer.
				// This was you can control what links the crawler should not follow
					new Dictionary<string, string>
						{
							{"<head", "</head>"}
						}),
				new IndexerDemo())
				{
					MaximumThreadCount = 2
				}) // Custom Step to send filtered content to index
			{
				// Begin crawl
				c.Crawl();
			}
		}

		#endregion
	}
}