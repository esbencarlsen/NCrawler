using System;
using System.Text.RegularExpressions;

using NCrawler.HtmlProcessor.Interfaces;

namespace NCrawler.HtmlProcessor
{
	public class RegexSubstitution : ISubstitution
	{
		private readonly Lazy<Regex> _match;
		private readonly string _replacement;

		public RegexSubstitution(Regex match, string replacement)
		{
			_match = new Lazy<Regex>(() => match, true);
			_replacement = replacement;
		}

		public string Substitute(string original, CrawlStep crawlStep)
		{
			return _match.Value.Replace(original, _replacement);
		}
	}
}