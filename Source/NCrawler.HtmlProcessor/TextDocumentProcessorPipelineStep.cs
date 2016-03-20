using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

		private const string RegexPattern = @"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
		private readonly Regex _urlMatcher = new Regex(RegexPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled);

		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode == HttpStatusCode.OK
				&& IsTextContent(propertyBag.ContentType))
			{
				string content = Encoding.UTF8.GetString(propertyBag.Response);
				propertyBag.Title = propertyBag.Step.Uri.ToString();
				propertyBag.Text = content.Trim();
				MatchCollection urlMatches = _urlMatcher.Matches(propertyBag.Text);
				foreach (Match urlMatch in urlMatches)
				{
					Uri uri;
					if (Uri.TryCreate(urlMatch.Value, UriKind.Absolute, out uri))
					{
						crawler.Crawl(uri, propertyBag);
					}
				}
			}

			return Task.FromResult(true);
		}

		public int MaxDegreeOfParallelism { get; }

		#endregion
	}
}