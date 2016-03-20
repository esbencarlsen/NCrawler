using System;

namespace NCrawler.LanguageDetection.Google
{
	public static class CrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration DetectLanguage(this CrawlerConfiguration crawlerConfiguration,
			int? maxDegreeOfParallelism = null)
		{
			crawlerConfiguration.AddPipelineStep(
				new GoogleLanguageDetection(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
			return crawlerConfiguration;
		}
	}
}