using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

using NCrawler.Extensions;
using NCrawler.HtmlProcessor.Extensions;
using NCrawler.HtmlProcessor.Properties;
using NCrawler.Interfaces;

namespace NCrawler.SitemapProcessor
{
	/// <summary>
	/// Courtesy of Muttok
	/// </summary>
	public class SitemapProcessor : IPipelineStep
	{
		#region Instance Methods

		protected virtual string NormalizeLink(string baseUrl, string link)
		{
			return link.NormalizeUrl(baseUrl);
		}

		#endregion

		#region IPipelineStep Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK)
			{
				return;
			}

			if (!IsXmlContent(propertyBag.ContentType))
			{
				return;
			}

			using (Stream reader = propertyBag.GetResponse())
			using (StreamReader sr = new StreamReader(reader))
			{
				XDocument mydoc = XDocument.Load(sr);
				if (mydoc.Root == null)
				{
					return;
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

					crawler.AddStep(new Uri(normalizedLink), propertyBag.Step.Depth + 1,
						propertyBag.Step, new Dictionary<string, object>
							{
								{Resources.PropertyBagKeyOriginalUrl, url},
								{Resources.PropertyBagKeyOriginalReferrerUrl, propertyBag.ResponseUri}
							});
				}
			}
		}

		#endregion

		#region Class Methods

		private static bool IsXmlContent(string contentType)
		{
			return contentType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}