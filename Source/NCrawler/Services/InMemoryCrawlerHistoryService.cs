using System.Collections.Generic;

using NCrawler.Utils;

namespace NCrawler.Services
{
	public class InMemoryCrawlerHistoryService : HistoryServiceBase
	{
		#region Readonly & Static Fields

		private readonly HashSet<string> _visitedUrls = new HashSet<string>();

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			_visitedUrls.Add(key);
		}

		protected override bool Exists(string key)
		{
			return _visitedUrls.Contains(key);
		}

		protected override long GetRegisteredCount()
		{
			return _visitedUrls.Count;
		}

		#endregion
	}
}