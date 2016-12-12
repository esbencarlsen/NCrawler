using System;

namespace NCrawler.PDFBox
{
	public static class CrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration PdfTextExtractProcessor(this CrawlerConfiguration crawlerConfiguration,
			int? maxDegreeOfParallelism = null)
		{
			PdfBoxTextExtractorProcessorPipelineStep filterTextExtractorProcessor =
				new PdfBoxTextExtractorProcessorPipelineStep(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount));
			crawlerConfiguration.AddPipelineStep(filterTextExtractorProcessor);
			return crawlerConfiguration;
		}
	}
}