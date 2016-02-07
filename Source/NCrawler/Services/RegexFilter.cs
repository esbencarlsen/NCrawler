using System;
using System.Text.RegularExpressions;

using NCrawler.Interfaces;

namespace NCrawler.Services
{
	public class RegexFilter : IFilter
	{
		#region Readonly & Static Fields

		private readonly Lazy<Regex> _regex;

		#endregion

		#region Constructors

		public RegexFilter(Regex regex)
		{
			_regex = new Lazy<Regex>(() => regex, true);
		}

		#endregion

		#region Operators

		public static explicit operator RegexFilter(Regex regex)
		{
			return new RegexFilter(regex);
		}

		#endregion

		#region IFilter Members

		public bool Match(Uri uri, CrawlStep referrer)
		{
			return _regex.Value.Match(uri.ToString()).Success;
		}

		#endregion
	}
}