using System;
using System.Text.RegularExpressions;

using NCrawler.HtmlProcessor.Interfaces;

namespace NCrawler.HtmlProcessor
{
	public class RegexSubstitution : ISubstitution
	{
		#region Readonly & Static Fields

		private readonly Lazy<Regex> _match;
		private readonly string _replacement;

		#endregion

		#region Constructors

		public RegexSubstitution(Regex match, string replacement)
		{
			_match = new Lazy<Regex>(() => match, true);
			_replacement = replacement;
		}

		#endregion

		#region ISubstitution Members

		public string Substitute(string original, CrawlStep crawlStep)
		{
			return _match.Value.Replace(original, _replacement);
		}

		#endregion
	}
}