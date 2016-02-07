using System.Threading;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.Utils
{
	public abstract class CrawlerQueueServiceBase : DisposableBase, ICrawlerQueue
	{
		#region Readonly & Static Fields

		private readonly ReaderWriterLockSlim m_QueueLock =
			new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		#endregion

		#region Instance Methods

		protected abstract long GetCount();
		protected abstract CrawlerQueueEntry PopImpl();
		protected abstract void PushImpl(CrawlerQueueEntry crawlerQueueEntry);

		protected override void Cleanup()
		{
			m_QueueLock.Dispose();
		}

		#endregion

		#region ICrawlerQueue Members

		public CrawlerQueueEntry Pop()
		{
			return AspectF.Define.
				WriteLock(m_QueueLock).
				Return<CrawlerQueueEntry>(PopImpl);
		}

		public void Push(CrawlerQueueEntry crawlerQueueEntry)
		{
			AspectF.Define.
				WriteLock(m_QueueLock).
				Do(() => PushImpl(crawlerQueueEntry));
		}

		public long Count
		{
			get
			{
				return AspectF.Define.
					ReadLock(m_QueueLock).
					Return<long>(GetCount);
			}
		}

		#endregion
	}
}