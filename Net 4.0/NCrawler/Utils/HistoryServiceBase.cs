using System.Threading;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.Utils
{
	public abstract class HistoryServiceBase : DisposableBase, ICrawlerHistory
	{
		#region Readonly & Static Fields

		private readonly ReaderWriterLockSlim m_CrawlHistoryLock =
			new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		#endregion

		#region Instance Methods

		protected abstract void Add(string key);
		protected abstract bool Exists(string key);
		protected abstract long GetRegisteredCount();

		protected override void Cleanup()
		{
			m_CrawlHistoryLock.Dispose();
		}

		#endregion

		#region ICrawlerHistory Members

		public virtual long RegisteredCount
		{
			get
			{
				return AspectF.Define.
					ReadLock(m_CrawlHistoryLock).
					Return(() => GetRegisteredCount());
			}
		}

		public virtual bool Register(string key)
		{
			return AspectF.Define.
				NotNullOrEmpty(key, "key").
				ReadLockUpgradable(m_CrawlHistoryLock).
				Return(() =>
					{
						bool exists = Exists(key);
						if (!exists)
						{
							AspectF.Define.
								WriteLock(m_CrawlHistoryLock).
								Do(() => Add(key));
						}

						return !exists;
					});
		}

		#endregion
	}
}