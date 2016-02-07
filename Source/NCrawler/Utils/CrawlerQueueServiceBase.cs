using System.Threading;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.Utils
{
	public abstract class CrawlerQueueServiceBase : DisposableBase, ICrawlerQueue
	{
		#region Readonly & Static Fields

		private readonly ReaderWriterLockSlim _queueLock =
			new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		#endregion

		#region Instance Methods

		protected abstract long GetCount();
		protected abstract CrawlerQueueEntry PopImpl();
		protected abstract void PushImpl(CrawlerQueueEntry crawlerQueueEntry);

		protected override void Cleanup()
		{
			_queueLock.Dispose();
		}

		#endregion

		#region ICrawlerQueue Members

		public CrawlerQueueEntry Pop()
		{
			return AspectF.Define.
				WriteLock(_queueLock).
				Return<CrawlerQueueEntry>(PopImpl);
		}

		public void Push(CrawlerQueueEntry crawlerQueueEntry)
		{
			AspectF.Define.
				WriteLock(_queueLock).
				Do(() => PushImpl(crawlerQueueEntry));
		}

		public long Count
		{
			get
			{
				return AspectF.Define.
					ReadLock(_queueLock).
					Return<long>(GetCount);
			}
		}

		#endregion
	}
}