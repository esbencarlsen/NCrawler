using System;
using System.Linq;

using NCrawler.Utils;

namespace NCrawler.DbServices
{
	public class DbCrawlerHistoryService : HistoryServiceBase
	{
		#region Readonly & Static Fields

		private readonly int m_GroupId;
		private readonly bool m_Resume;

		#endregion

		#region Constructors

		public DbCrawlerHistoryService(Uri uri, bool resume)
		{
			m_Resume = resume;
			m_GroupId = uri.GetHashCode();

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
						e.AddToCrawlHistory(CrawlHistory.CreateCrawlHistory(0, key, m_GroupId));
						e.SaveChanges();
					});
		}

		protected override void Cleanup()
		{
			if (!m_Resume)
			{
				Clean();
			}

			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return AspectF.Define.
				Return<bool, NCrawlerEntitiesDbServices>(
					e => e.CrawlHistory.Where(h => h.GroupId == m_GroupId && h.Key == key).Any());
		}

		protected override long GetRegisteredCount()
		{
			return AspectF.Define.
				Return<long, NCrawlerEntitiesDbServices>(e => e.CrawlHistory.Count(h => h.GroupId == m_GroupId));
		}

		private void Clean()
		{
#if !DOTNET4
			using (NCrawlerEntitiesDbServices e = new NCrawlerEntitiesDbServices())
			{
				foreach(CrawlHistory historyObject in e.CrawlHistory.Where(h => h.GroupId == m_GroupId))
				{
					e.DeleteObject(historyObject);
				}

				e.SaveChanges();
			}
#else
			AspectF.Define.
				Do<NCrawlerEntitiesDbServices>(e => e.ExecuteStoreCommand("DELETE FROM CrawlHistory WHERE GroupId = {0}", m_GroupId));
#endif
		}

		#endregion
	}
}