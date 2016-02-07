using System;

using NCrawler;
using NCrawler.Events;
using NCrawler.HtmlProcessor;

namespace NGet
{
	internal class Program
	{
		#region Readonly & Static Fields

		private static readonly Arguments arguments = new Arguments();

		#endregion

		#region Class Methods

		private static void Main(string[] args)
		{
			if (args == null || args.Length == 0)
			{
				arguments.ShowUsageShort();
			}
			else
			{
				arguments.m_StartupArgumentOptionSet.Parse(args);

				using (Crawler crawler = new Crawler(new Uri("http://ncrawler.codeplex.com"),
					new HtmlDocumentProcessor(),
					new ConsolePipelineStep()))
				{
					crawler.MaximumThreadCount = 10;
					crawler.Cancelled += crawler_Cancelled;
					crawler.DownloadException += crawler_DownloadException;
					crawler.DownloadProgress += crawler_DownloadProgress;
					crawler.PipelineException += crawler_PipelineException;
					crawler.Crawl();
				}
			}
		}

		private static void crawler_Cancelled(object sender, EventArgs e)
		{
			arguments.DefaultOutput.WriteLine("Cancelled");
		}

		private static void crawler_DownloadException(object sender, DownloadExceptionEventArgs e)
		{
			arguments.DefaultOutput.WriteLine("Download Exception");
		}

		private static void crawler_DownloadProgress(object sender, DownloadProgressEventArgs e)
		{
			//arguments.DefaultOutput.Write("{0} - {1}\r".FormatWith(e.Step.Uri, e.PercentCompleted));
		}

		private static void crawler_PipelineException(object sender, PipelineExceptionEventArgs e)
		{
			arguments.DefaultOutput.WriteLine("Pipeline Exception - {0}", e.Exception);
		}

		#endregion
	}
}