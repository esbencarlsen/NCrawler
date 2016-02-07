using System;

using NCrawler;
using NCrawler.Events;
using NCrawler.HtmlProcessor;

namespace NGet
{
	internal class Program
	{
		#region Readonly & Static Fields

		private static readonly Arguments s_arguments = new Arguments();

		#endregion

		#region Class Methods

		private static void Main(string[] args)
		{
			if (args == null || args.Length == 0)
			{
				s_arguments.ShowUsageShort();
			}
			else
			{
				s_arguments.StartupArgumentOptionSet.Parse(args);

				using (Crawler crawler = new Crawler(new Uri("http://ncrawler.codeplex.com"),
					new HtmlDocumentProcessor(),
					new ConsolePipelineStep()))
				{
					crawler.MaximumThreadCount = 10;
					crawler.Cancelled += CrawlerCancelled;
					crawler.DownloadException += CrawlerDownloadException;
					crawler.DownloadProgress += CrawlerDownloadProgress;
					crawler.PipelineException += CrawlerPipelineException;
					crawler.Crawl();
				}
			}
		}

		private static void CrawlerCancelled(object sender, EventArgs e)
		{
			s_arguments.DefaultOutput.WriteLine("Cancelled");
		}

		private static void CrawlerDownloadException(object sender, DownloadExceptionEventArgs e)
		{
			s_arguments.DefaultOutput.WriteLine("Download Exception");
		}

		private static void CrawlerDownloadProgress(object sender, DownloadProgressEventArgs e)
		{
			//arguments.DefaultOutput.Write("{0} - {1}\r".FormatWith(e.Step.Uri, e.PercentCompleted));
		}

		private static void CrawlerPipelineException(object sender, PipelineExceptionEventArgs e)
		{
			s_arguments.DefaultOutput.WriteLine("Pipeline Exception - {0}", e.Exception);
		}

		#endregion
	}
}