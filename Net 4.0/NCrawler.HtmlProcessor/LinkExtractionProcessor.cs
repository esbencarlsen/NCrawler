using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NCrawler.Extensions;
using NCrawler.HtmlProcessor.Properties;
using NCrawler.Interfaces;

namespace NCrawler.HtmlProcessor
{
	public class LinkExtractionProcessor : ContentCrawlerRules, IPipelineStep
	{
		#region Readonly & Static Fields

		private static readonly Lazy<Regex> s_LinkRegex = new Lazy<Regex>(() => new Regex(
			"(?<Protocol>\\w+):\\/\\/(?<Domain>[\\w@][\\w.:@]+)\\/?[\\w\\.?=%&=\\-@/$,]*",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant |
				RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled), true);

		#endregion

		#region Constructors

		public LinkExtractionProcessor()
		{
		}

		public LinkExtractionProcessor(Dictionary<string, string> filterTextRules, Dictionary<string, string> filterLinksRules)
			: base(filterTextRules, filterLinksRules)
		{
		}

		#endregion

		#region IPipelineStep Members

		public virtual void Process(Crawler crawler, PropertyBag propertyBag)
		{
			// Get text from previous pipeline step
			string text = propertyBag.Text;
			if (HasTextStripRules)
			{
				text = StripText(text);
			}

			if (text.IsNullOrEmpty())
			{
				return;
			}

			if (HasLinkStripRules)
			{
				text = StripLinks(text);
			}

			// Find links
			MatchCollection matches = s_LinkRegex.Value.Matches(text);
			foreach (Match match in matches.Cast<Match>().Where(m => m.Success))
			{
				string link = match.Value;
				if (link.IsNullOrEmpty())
				{
					continue;
				}

				string baseUrl = propertyBag.ResponseUri.GetLeftPart(UriPartial.Path);
				string normalizedLink = link.NormalizeUrl(baseUrl);
				if (normalizedLink.IsNullOrEmpty())
				{
					continue;
				}

				// Add new step to crawler
				crawler.AddStep(new Uri(normalizedLink), propertyBag.Step.Depth + 1,
					propertyBag.Step, new Dictionary<string, object>
						{
							{Resources.PropertyBagKeyOriginalUrl, new Uri(link)},
							{Resources.PropertyBagKeyOriginalReferrerUrl, propertyBag.ResponseUri}
						});
			}
		}

		#endregion
	}
}