using System;

namespace NCrawler.Toxy
{
	public static class CrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration TextExtractProcessor(this CrawlerConfiguration crawlerConfiguration,
			int? maxDegreeOfParallelism = null)
		{
			ToxyTextExtractorProcessorPipelineStep filterTextExtractorProcessor =
				new ToxyTextExtractorProcessorPipelineStep(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount));
			crawlerConfiguration.AddPipelineStep(filterTextExtractorProcessor);
			return crawlerConfiguration;
		}
	}
}