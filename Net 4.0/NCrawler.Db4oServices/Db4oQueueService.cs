using System;
using System.IO;
using System.Linq;

using Db4objects.Db4o;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.Db4oServices
{
	public class Db4oQueueService : CrawlerQueueServiceBase
	{
		#region Readonly & Static Fields

		private readonly IObjectContainer m_Db;

		#endregion

		#region Constructors

		public Db4oQueueService(Uri baseUri, bool resume)
		{
			string fileName = Path.GetFullPath("NCrawlerQueue_{0}.Yap".FormatWith(baseUri.GetHashCode()));
			m_Db = Db4oEmbedded.OpenFile(Db4oEmbedded.NewConfiguration(), fileName);

			if (!resume)
			{
				ClearQueue();
			}
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			m_Db.Dispose();
			base.Cleanup();
		}

		protected override long GetCount()
		{
			return m_Db.Query<CrawlerQueueEntry>().Count;
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			CrawlerQueueEntry result = m_Db.Query<CrawlerQueueEntry>().FirstOrDefault();
			if (result.IsNull())
			{
				return null;
			}

			m_Db.Delete(result);
			return result;
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			m_Db.Store(crawlerQueueEntry);
		}

		private void ClearQueue()
		{
			m_Db.Query<CrawlerQueueEntry>().ForEach(entry => m_Db.Delete(entry));
		}

		#endregion
	}
}