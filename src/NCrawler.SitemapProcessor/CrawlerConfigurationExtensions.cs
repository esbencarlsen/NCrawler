using System;

namespace NCrawler.SitemapProcessor
{
	public static class CrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration PdfTextExtractProcessor(this CrawlerConfiguration crawlerConfiguration,
			int? maxDegreeOfParallelism = null)
		{
			SitemapProcessor sitemapProcessor =
				new SitemapProcessor(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount));
			crawlerConfiguration.AddPipelineStep(sitemapProcessor);
			return crawlerConfiguration;
		}
	}
}