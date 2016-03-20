using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using NCrawler.Extensions;
using NCrawler.Utils;

using Toxy;

namespace NCrawler.Toxy
{
	public class ToxyTextExtractorProcessorPipelineStep : IPipelineStep
	{
		public readonly Dictionary<string, string> MimeTypeExtensionMapping = new Dictionary<string, string>
		{
			{"application/excel", "xls"},
			{"application/vnd.ms-excel", "xsl"},
			{"application/x-msexcel", "xsl"},
			{"application/word", "doc"},
			{"application/msword", "doc"}
		};

		public ToxyTextExtractorProcessorPipelineStep(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public async Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK
				|| propertyBag.Response == null)
			{
				return true;
			}

			string extension = MapContentTypeToExtension(propertyBag.ContentType);
			if (extension.IsNullOrEmpty())
			{
				return true;
			}

			propertyBag.Title = propertyBag.Step.Uri.PathAndQuery;
			using (TempFile temp = new TempFile())
			{
				temp.FileName += "." + extension;
				using (FileStream fs = new FileStream(temp.FileName, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000))
				{
					await fs.WriteAsync(propertyBag.Response, 0, propertyBag.Response.Length);
				}

				ParserContext context = new ParserContext(temp.FileName);
				ITextParser parser = ParserFactory.CreateText(context);
				propertyBag.Text = parser.Parse();
			}

			return true;
		}

		public int MaxDegreeOfParallelism { get; }

		protected virtual string MapContentTypeToExtension(string mimeType)
		{
			mimeType = mimeType.ToLowerInvariant();
			string extension;
			return MimeTypeExtensionMapping.TryGetValue(mimeType, out extension) ? extension : null;
		}
	}
}