using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using NCrawler.Interfaces;

namespace NCrawler.HtmlProcessor
{
	public class TextDocumentProcessorPipelineStep : IPipelineStep
	{
		public TextDocumentProcessorPipelineStep(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		#region Class Methods

		private static bool IsTextContent(string contentType)
		{
			return contentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region IPipelineStep Members

		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode == HttpStatusCode.OK
				&& IsTextContent(propertyBag.ContentType))
			{
				string content = Encoding.UTF8.GetString(propertyBag.Response);
				propertyBag.Text = content.Trim();
			}

			return Task.FromResult(true);
		}

		public int MaxDegreeOfParallelism { get; }

		#endregion
	}
}