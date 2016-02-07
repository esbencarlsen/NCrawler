using System;
using System.IO;
using System.Linq;

using Db4objects.Db4o;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.Db4oServices
{
	public class Db4OQueueService : CrawlerQueueServiceBase
	{
		#region Readonly & Static Fields

		private readonly IObjectContainer _db;

		#endregion

		#region Constructors

		public Db4OQueueService(Uri baseUri, bool resume)
		{
			string fileName = Path.GetFullPath("NCrawlerQueue_{0}.Yap".FormatWith(baseUri.GetHashCode()));
			_db = Db4oEmbedded.OpenFile(Db4oEmbedded.NewConfiguration(), fileName);

			if (!resume)
			{
				ClearQueue();
			}
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			_db.Dispose();
			base.Cleanup();
		}

		protected override long GetCount()
		{
			return _db.Query<CrawlerQueueEntry>().Count;
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			CrawlerQueueEntry result = _db.Query<CrawlerQueueEntry>().FirstOrDefault();
			if (result.IsNull())
			{
				return null;
			}

			_db.Delete(result);
			return result;
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			_db.Store(crawlerQueueEntry);
		}

		private void ClearQueue()
		{
			_db.Query<CrawlerQueueEntry>().ForEach(entry => _db.Delete(entry));
		}

		#endregion
	}
}