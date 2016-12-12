using System.Net;
using System.Threading.Tasks;

using java.io;

using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.util;

namespace NCrawler.PDFBox
{
	public class PdfBoxTextExtractorProcessorPipelineStep : IPipelineStep
	{
		public PdfBoxTextExtractorProcessorPipelineStep(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK
				|| propertyBag.Response == null)
			{
				return Task.FromResult(true);
			}

			PDDocument doc = null;
			try
			{
				doc = PDDocument.load(new ByteArrayInputStream(propertyBag.Response));
				PDFTextStripper stripper = new PDFTextStripper();
				propertyBag.Text = stripper.getText(doc);
			}
			finally
			{
				doc?.close();
			}

			return Task.FromResult(true);
		}

		public int MaxDegreeOfParallelism { get; }
	}
}