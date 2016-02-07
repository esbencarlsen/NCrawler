using System;

namespace NCrawler.Interfaces
{
	public interface IFilter
	{
		#region Instance Methods

		bool Match(Uri uri, CrawlStep referrer);

		#endregion
	}
}