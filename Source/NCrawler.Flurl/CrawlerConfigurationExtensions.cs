using System;

namespace NCrawler.Flurl
{
	public static class CrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration FlurlDownload(this CrawlerConfiguration crawlerConfiguration,
			int? maxDegreeOfParallelism = null)
		{
			crawlerConfiguration.AddPipelineStep(
				new FlurlDownloadPipelineStep(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
			return crawlerConfiguration;
		}
	}
}