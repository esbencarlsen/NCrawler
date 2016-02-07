// --------------------------------------------------------------------------------------------------------------------- 
// <copyright file="EMailEntityExtractionProcessor.cs" company="">
//   
// </copyright>
// <summary>
//   Defines the EMailEntityExtractionProcessor type.
// </summary>
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Utils;

namespace NCrawler.HtmlProcessor
{
	public class EMailEntityExtractionProcessor : IPipelineStep
	{
		#region Constants

		private const RegexOptions Options = RegexOptions.IgnoreCase |
			RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled;

		#endregion

		#region Readonly & Static Fields

		private static readonly Lazy<Regex> s_EmailRegex = new Lazy<Regex>(() => new Regex(
			"([a-zA-Z0-9_\\-\\.]+)@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1" +
				",3}\\.)|(([a-zA-Z0-9\\-]+\\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})",
			Options), true);

		#endregion

		#region IPipelineStep Members

		/// <summary>
		/// </summary>
		/// <param name="crawler">
		/// The crawler.
		/// </param>
		/// <param name="propertyBag">
		/// The property bag.
		/// </param>
		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			AspectF.Define.
				NotNull(crawler, "crawler").
				NotNull(propertyBag, "propertyBag");

			string text = propertyBag.Text;
			if (text.IsNullOrEmpty())
			{
				return;
			}

			MatchCollection matches = s_EmailRegex.Value.Matches(text);
			propertyBag["Email"].Value = matches.Cast<Match>().
				Select(match => match.Value).
				Join(";");
		}

		#endregion
	}
}