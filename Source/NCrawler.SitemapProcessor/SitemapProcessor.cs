using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;

using NCrawler.Extensions;
using NCrawler.HtmlProcessor.Extensions;

namespace NCrawler.SitemapProcessor
{
	/// <summary>
	///     Courtesy of Muttok
	/// </summary>
	public class SitemapProcessor : IPipelineStep
	{
		public SitemapProcessor(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK
				|| propertyBag.Response == null
				|| propertyBag.Response.Length == 0)
			{
				return Task.FromResult(true);
			}

			if (!IsXmlContent(propertyBag.ContentType))
			{
				return Task.FromResult(true);
			}

			using (MemoryStream ms = new MemoryStream(propertyBag.Response))
			{
				XDocument mydoc = XDocument.Load(ms);
				if (mydoc.Root == null)
				{
					return Task.FromResult(true);
				}

				XName qualifiedName = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
				IEnumerable<string> urlNodes =
					from e in mydoc.Descendants(qualifiedName)
					where !e.Value.IsNullOrEmpty() && e.Value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
					select e.Value;

				foreach (string url in urlNodes)
				{
					// add new crawler steps
					string baseUrl = propertyBag.ResponseUri.GetLeftPart(UriPartial.Path);
					string decodedLink = ExtendedHtmlUtility.HtmlEntityDecode(url);
					string normalizedLink = NormalizeLink(baseUrl, decodedLink);
					if (normalizedLink.IsNullOrEmpty())
					{
						continue;
					}

					propertyBag["PropertyBagKeyOriginalUrl"].Value = url;
					propertyBag["PropertyBagKeyOriginalReferrerUrl"].Value = propertyBag.ResponseUri;
					crawler.Crawl(new Uri(normalizedLink), propertyBag);
				}
			}

			return Task.FromResult(true);
		}

		public int MaxDegreeOfParallelism { get; }

		protected virtual string NormalizeLink(string baseUrl, string link)
		{
			return link.NormalizeUrl(baseUrl);
		}

		private static bool IsXmlContent(string contentType)
		{
			return contentType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase);
		}
	}
}