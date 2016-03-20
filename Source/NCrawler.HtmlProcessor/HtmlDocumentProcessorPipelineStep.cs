using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using NCrawler.Extensions;
using NCrawler.HtmlProcessor.Extensions;
using NCrawler.Utils;

namespace NCrawler.HtmlProcessor
{
	public class HtmlDocumentProcessorPipelineStep : ContentCrawlerRules, IPipelineStep
	{
		public HtmlDocumentProcessorPipelineStep(int maxDegreeOfParallelism) : this(maxDegreeOfParallelism, null, null)
		{
		}

		public HtmlDocumentProcessorPipelineStep(
			int maxDegreeOfParallelism,
			Dictionary<string, string> filterTextRules,
			Dictionary<string, string> filterLinksRules)
			: base(filterTextRules, filterLinksRules)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			AspectF.Define
				.NotNull(crawler, nameof(crawler))
				.NotNull(propertyBag, nameof(propertyBag));

			if (propertyBag.StatusCode != HttpStatusCode.OK)
			{
				return Task.FromResult(true);
			}

			if (!IsHtmlContent(propertyBag.ContentType))
			{
				return Task.FromResult(true);
			}

			HtmlDocument htmlDoc = new HtmlDocument
			{
				OptionAddDebuggingAttributes = false,
				OptionAutoCloseOnEnd = true,
				OptionFixNestedTags = true,
				OptionReadEncoding = true
			};

			using (MemoryStream ms = new MemoryStream(propertyBag.Response))
			{
				Encoding documentEncoding = htmlDoc.DetectEncoding(ms);
				ms.Seek(0, SeekOrigin.Begin);
				if (!documentEncoding.IsNull())
				{
					htmlDoc.Load(ms, documentEncoding, true);
				}
				else
				{
					htmlDoc.Load(ms, true);
				}
			}

			string originalContent = htmlDoc.DocumentNode.OuterHtml;
			if (HasTextStripRules || HasSubstitutionRules)
			{
				string content = StripText(originalContent);
				content = Substitute(content, propertyBag.Step);
				using (TextReader tr = new StringReader(content))
				{
					htmlDoc.Load(tr);
				}
			}

			propertyBag["HtmlDoc"].Value = htmlDoc;

			HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//title");
			// Extract Title
			if (!nodes.IsNull())
			{
				propertyBag.Title = string.Join(";", nodes.
					Select(n => n.InnerText).
					ToArray()).Trim();
			}

			// Extract Meta Data
			nodes = htmlDoc.DocumentNode.SelectNodes("//meta[@content and @name]");
			if (!nodes.IsNull())
			{
				propertyBag["Meta"].Value = (
					from entry in nodes
					let name = entry.Attributes["name"]
					let content = entry.Attributes["content"]
					where !name.IsNull() && !name.Value.IsNullOrEmpty() && !content.IsNull() && !content.Value.IsNullOrEmpty()
					select $"{name.Value}: {content.Value}").ToArray();
			}

			// Extract text
			propertyBag.Text = htmlDoc.ExtractText().Trim();
			if (HasLinkStripRules || HasTextStripRules)
			{
				string content = StripLinks(originalContent);
				using (TextReader tr = new StringReader(content))
				{
					htmlDoc.Load(tr);
				}
			}

			string baseUrl = propertyBag.ResponseUri.GetLeftPart(UriPartial.Path);

			// Extract Head Base
			nodes = htmlDoc.DocumentNode.SelectNodes("//head/base[@href]");
			if (!nodes.IsNull())
			{
				baseUrl = nodes
					.Select(entry => new {entry, href = entry.Attributes["href"]})
					.Where(arg => !arg.href.IsNull()
						&& !arg.href.Value.IsNullOrEmpty()
						&& Uri.IsWellFormedUriString(arg.href.Value, UriKind.RelativeOrAbsolute))
					.Select(t =>
					{
						if (Uri.IsWellFormedUriString(t.href.Value, UriKind.Relative))
						{
							return propertyBag.ResponseUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) + t.href.Value;
						}

						return t.href.Value;
					})
					.AddToEnd(baseUrl)
					.FirstOrDefault();
			}

			// Extract Links
			DocumentWithLinks links = htmlDoc.GetLinks();
			foreach (string link in links.Links.Union(links.References))
			{
				if (link.IsNullOrEmpty())
				{
					continue;
				}

				string decodedLink = ExtendedHtmlUtility.HtmlEntityDecode(link);
				string normalizedLink = NormalizeLink(baseUrl, decodedLink);
				if (normalizedLink.IsNullOrEmpty())
				{
					continue;
				}

				crawler.Crawl(new Uri(normalizedLink), propertyBag);
			}

			return Task.FromResult(true);
		}

		public int MaxDegreeOfParallelism { get; }

		protected virtual string NormalizeLink(string baseUrl, string link)
		{
			return link.NormalizeUrl(baseUrl);
		}

		private static bool IsHtmlContent(string contentType)
		{
			return contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
		}
	}
}