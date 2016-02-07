using System.Threading;

namespace NCrawler.Utils
{
	internal class ThreadSafeCounter
	{
		#region Fields

		private long m_Counter;

		#endregion

		#region Instance Properties

		public long Value
		{
			get { return Interlocked.Read(ref m_Counter); }
		}

		#endregion

		#region Instance Methods

		public ThreadSafeCounterCookie EnterCounterScope(CrawlerQueueEntry crawlerQueueEntry)
		{
			Increment();
			return new ThreadSafeCounterCookie(this, crawlerQueueEntry);
		}

		private void Decrement()
		{
			Interlocked.Decrement(ref m_Counter);
		}

		private void Increment()
		{
			Interlocked.Increment(ref m_Counter);
		}

		#endregion

		#region Nested type: ThreadSafeCounterCookie

		internal class ThreadSafeCounterCookie : DisposableBase
		{
			#region Readonly & Static Fields

			private readonly ThreadSafeCounter m_ThreadSafeCounter;

			#endregion

			#region Constructors

			public ThreadSafeCounterCookie(ThreadSafeCounter threadSafeCounter, CrawlerQueueEntry crawlerQueueEntry)
			{
				m_ThreadSafeCounter = threadSafeCounter;
				CrawlerQueueEntry = crawlerQueueEntry;
			}

			#endregion

			#region Instance Methods

			public CrawlerQueueEntry CrawlerQueueEntry { get; private set; }

			protected override void Cleanup()
			{
				m_ThreadSafeCounter.Decrement();
			}

			#endregion
		}

		#endregion
	}
}