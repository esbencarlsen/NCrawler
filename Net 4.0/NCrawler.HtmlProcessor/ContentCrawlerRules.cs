// --------------------------------------------------------------------------------------------------------------------- 
// <copyright file="CrawlerRules.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the CrawlerRules type.
// </summary>
// ---------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NCrawler.Extensions;
using NCrawler.HtmlProcessor.Interfaces;

namespace NCrawler.HtmlProcessor
{
	/// <summary>
	/// Class for filtering content, for example you might wan't to exclude partial
	/// link contained in a speciel part of a html page from beeing followed.
	/// Or maybe exclude part of a textual content
	/// And replacecontent based regex
	/// </summary>
	public abstract class ContentCrawlerRules
	{
		#region Readonly & Static Fields

		/// <summary>
		/// </summary>
		private readonly Dictionary<string, string> m_FilterLinksRules;

		/// <summary>
		/// </summary>
		private readonly Dictionary<string, string> m_FilterTextRules;

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ContentCrawlerRules"/> class. 
		/// </summary>
		protected ContentCrawlerRules()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ContentCrawlerRules"/> class. 
		/// </summary>
		/// <param name="filterTextRules">
		/// The filter text rules.
		/// </param>
		/// <param name="filterLinksRules">
		/// The filter links rules.
		/// </param>
		protected ContentCrawlerRules(Dictionary<string, string> filterTextRules, Dictionary<string, string> filterLinksRules)
		{
			m_FilterTextRules = filterTextRules;
			m_FilterLinksRules = filterLinksRules;
		}

		#endregion

		#region Instance Properties

		public IEnumerable<ISubstitution> Substitutions { get; set; }

		/// <summary>
		/// Gets a value indicating whether Has Link Strip Rules.
		/// </summary>
		/// <value>
		/// The has rules.
		/// </value>
		protected bool HasLinkStripRules
		{
			get { return !m_FilterLinksRules.IsNull() && m_FilterLinksRules.Count > 0; }
		}

		/// <summary>
		/// Gets a value indicating whether HasTextStripRules.
		/// </summary>
		/// <value>
		/// The has rules.
		/// </value>
		protected bool HasTextStripRules
		{
			get { return !m_FilterTextRules.IsNull() && m_FilterTextRules.Count > 0; }
		}

		protected bool HasSubstitutionRules
		{
			get { return !Substitutions.IsNull(); }
		}

		#endregion

		#region Instance Methods

		/// <summary>
		/// </summary>
		/// <param name="content">
		/// The content.
		/// </param>
		/// <returns>
		/// </returns>
		protected string StripLinks(string content)
		{
			return StripByRules(m_FilterLinksRules, content);
		}

		/// <summary>
		/// </summary>
		/// <param name="content">
		/// The content.
		/// </param>
		/// <returns>
		/// </returns>
		protected string StripText(string content)
		{
			return StripByRules(m_FilterTextRules, content);
		}

		protected string Substitute(string original, CrawlStep crawlStep)
		{
			return HasSubstitutionRules
				? Substitutions.Aggregate(original, (current, substitution) => substitution.Substitute(current, crawlStep))
				: original;
		}

		#endregion

		#region Class Methods

		/// <summary>
		/// Basically strips everything between the start marker and the end marker
		/// The start marker is the Key in the Dictionary<string, string>, the end marker is the Value
		/// </summary>
		/// <param name="rules">
		/// </param>
		/// <param name="content">
		/// </param>
		/// <returns>
		/// </returns>
		private static string StripByRules(Dictionary<string, string> rules, string content)
		{
			if (rules.IsNull() || content.IsNullOrEmpty())
			{
				return content;
			}

			foreach (KeyValuePair<string, string> k in rules)
			{
				string key = Regex.Escape(k.Key);
				string value = Regex.Escape(k.Value);
				string pattern = "({0})(.*?)({1})".FormatWith(key, value);
				const RegexOptions options = RegexOptions.IgnoreCase |
					RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled;
				content = Regex.Replace(content, pattern, string.Empty, options);
			}

			return content;
		}

		#endregion
	}
}