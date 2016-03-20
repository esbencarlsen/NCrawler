using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using NCrawler.Extensions;
using NCrawler.Utils;

using NTextCat;

namespace NCrawler.LanguageDetection.Google
{
	public class GoogleLanguageDetection : IPipelineStep
	{
		public const string LanguagePropertyName = "Language";
		private readonly RankedLanguageIdentifier _identifier;

		public GoogleLanguageDetection(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;

			Assembly assembly = Assembly.GetExecutingAssembly();
			string resourceName = "NCrawler.LanguageDetection.Google.Core14.profile.xml";
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				RankedLanguageIdentifierFactory factory = new RankedLanguageIdentifierFactory();
				_identifier = factory.Load(stream);
			}
		}

		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			AspectF.Define.
				NotNull(crawler, "crawler").
				NotNull(propertyBag, "propertyBag");

			string content = propertyBag.Text;
			if (content.IsNullOrEmpty())
			{
				return Task.FromResult(true);
			}

			IEnumerable<Tuple<LanguageInfo, double>> languages = _identifier.Identify(content);
			Tuple<LanguageInfo, double> mostCertainLanguage = languages.FirstOrDefault();
			if (mostCertainLanguage != null)
			{
				propertyBag[LanguagePropertyName].Value = mostCertainLanguage.Item1.Iso639_3;
			}

			return Task.FromResult(true);
		}

		public int MaxDegreeOfParallelism { get; }
	}
}