using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Utils;

namespace NCrawler.Pipeline
{
	public class EMailEntityExtractionProcessor : IPipelineStep
	{
		#region Constants

		private const RegexOptions Options = RegexOptions.IgnoreCase |
			RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled;

		#endregion

		#region Readonly & Static Fields

		private static readonly Lazy<Regex> s_emailRegex = new Lazy<Regex>(() => new Regex(
			"([a-zA-Z0-9_\\-\\.]+)@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.)|(([a-zA-Z0-9\\-]+\\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})",
			Options), true);

		#endregion

		public EMailEntityExtractionProcessor(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public int MaxDegreeOfParallelism { get; }

		#region IPipelineStep Members

		/// <summary>
		/// </summary>
		/// The crawler.
		/// <param name="crawler"></param>
		/// <param name="propertyBag">
		///     The property bag.
		/// </param>
		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			AspectF.Define
				.NotNull(propertyBag, "propertyBag");

			string text = propertyBag.Text;
			if (!text.IsNullOrEmpty())
			{
				MatchCollection matches = s_emailRegex.Value.Matches(text);
				propertyBag["Email"].Value = matches
					.Cast<Match>()
					.Select(match => match.Value)
					.ToArray();
			}

			return Task.FromResult(true);
		}

		#endregion
	}
}