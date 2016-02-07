using System;
using System.Linq;

using NCrawler.Utils;

namespace NCrawler.DbServices
{
	public class DbCrawlerHistoryService : HistoryServiceBase
	{
		#region Readonly & Static Fields

		private readonly int _groupId;
		private readonly bool _resume;

		#endregion

		#region Constructors

		public DbCrawlerHistoryService(Uri uri, bool resume)
		{
			_resume = resume;
			_groupId = uri.GetHashCode();

			if (!resume)
			{
				Clean();
			}
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			AspectF.Define.
				Do<NCrawlerEntitiesDbServices>(e =>
					{
						e.AddToCrawlHistory(CrawlHistory.CreateCrawlHistory(0, key, _groupId));
						e.SaveChanges();
					});
		}

		protected override void Cleanup()
		{
			if (!_resume)
			{
				Clean();
			}

			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return AspectF.Define.
				Return<bool, NCrawlerEntitiesDbServices>(
					e => e.CrawlHistory.Where(h => h.GroupId == _groupId && h.Key == key).Any());
		}

		protected override long GetRegisteredCount()
		{
			return AspectF.Define.
				Return<long, NCrawlerEntitiesDbServices>(e => e.CrawlHistory.Count(h => h.GroupId == _groupId));
		}

		private void Clean()
		{
#if !DOTNET4
			using (NCrawlerEntitiesDbServices e = new NCrawlerEntitiesDbServices())
			{
				foreach(CrawlHistory historyObject in e.CrawlHistory.Where(h => h.GroupId == _GroupId))
				{
					e.DeleteObject(historyObject);
				}

				e.SaveChanges();
			}
#else
			AspectF.Define.
				Do<NCrawlerEntitiesDbServices>(e => e.ExecuteStoreCommand("DELETE FROM CrawlHistory WHERE GroupId = {0}", _groupId));
#endif
		}

		#endregion
	}
}