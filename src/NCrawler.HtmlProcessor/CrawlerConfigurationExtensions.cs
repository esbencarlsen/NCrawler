using System;

namespace NCrawler.HtmlProcessor
{
	public static class CrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration HtmlProcessor(this CrawlerConfiguration crawlerConfiguration,
			int? maxDegreeOfParallelism = null)
		{
			crawlerConfiguration.AddPipelineStep(
				new HtmlDocumentProcessorPipelineStep(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
			return crawlerConfiguration;
		}

		public static CrawlerConfiguration TextProcessor(this CrawlerConfiguration crawlerConfiguration,
			int? maxDegreeOfParallelism = null)
		{
			crawlerConfiguration.AddPipelineStep(
				new TextDocumentProcessorPipelineStep(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
			return crawlerConfiguration;
		}
	}
}