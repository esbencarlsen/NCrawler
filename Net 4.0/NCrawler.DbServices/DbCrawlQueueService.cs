using System;
using System.Linq;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.DbServices
{
	public class DbCrawlQueueService : CrawlerQueueServiceBase
	{
		#region Readonly & Static Fields

		private readonly int m_GroupId;

		#endregion

		#region Constructors

		public DbCrawlQueueService(Uri baseUri, bool resume)
		{
			m_GroupId = baseUri.GetHashCode();
			if (!resume)
			{
				Clean();
			}
		}

		#endregion

		#region Instance Methods

		protected override long GetCount()
		{
			return AspectF.Define.
				Return<long, NCrawlerEntitiesDbServices>(e => e.CrawlQueue.Count(q => q.GroupId == m_GroupId));
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			return AspectF.Define.
				Return<CrawlerQueueEntry, NCrawlerEntitiesDbServices>(e =>
					{
						CrawlQueue result = e.CrawlQueue.FirstOrDefault(q => q.GroupId == m_GroupId);
						if (result.IsNull())
						{
							return null;
						}

						e.DeleteObject(result);
						e.SaveChanges();
						return result.SerializedData.FromBinary<CrawlerQueueEntry>();
					});
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			AspectF.Define.
				Do<NCrawlerEntitiesDbServices>(e =>
					{
						e.AddToCrawlQueue(new CrawlQueue
							{
								GroupId = m_GroupId,
								SerializedData = crawlerQueueEntry.ToBinary(),
							});
						e.SaveChanges();
					});
		}

		private void Clean()
		{
#if !DOTNET4
			using (NCrawlerEntitiesDbServices e = new NCrawlerEntitiesDbServices())
			{
				foreach (CrawlQueue queueObject in e.CrawlQueue.Where(q => q.GroupId == m_GroupId))
				{
					e.DeleteObject(queueObject);
				}

				e.SaveChanges();
			}
#else
			AspectF.Define.
				Do<NCrawlerEntitiesDbServices>(e => e.ExecuteStoreCommand("DELETE FROM CrawlQueue WHERE GroupId = {0}", m_GroupId));
#endif
		}

		#endregion
	}
}