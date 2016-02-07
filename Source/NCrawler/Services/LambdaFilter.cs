using System;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.Services
{
	public class LambdaFilter : IFilter
	{
		#region Readonly & Static Fields

		private readonly Func<Uri, CrawlStep, bool> _match;
		private readonly Func<Uri, bool> _match2;

		#endregion

		#region Constructors

		public LambdaFilter(Func<Uri, CrawlStep, bool> match)
		{
			_match = match;
		}

		public LambdaFilter(Func<Uri, bool> match2)
		{
			_match2 = match2;
		}

		#endregion

		#region Operators

		public static explicit operator LambdaFilter(Func<Uri, bool> match)
		{
			return new LambdaFilter(match);
		}

		public static explicit operator LambdaFilter(Func<Uri, CrawlStep, bool> match)
		{
			return new LambdaFilter(match);
		}

		#endregion

		#region IFilter Members

		public bool Match(Uri uri, CrawlStep referrer)
		{
			if (!_match.IsNull())
			{
				return _match(uri, referrer);
			}

			if (!_match2.IsNull())
			{
				return _match2(uri);
			}

			return false;
		}

		#endregion
	}
}