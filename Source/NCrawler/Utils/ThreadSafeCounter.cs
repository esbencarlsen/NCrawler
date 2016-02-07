using System.Threading;

namespace NCrawler.Utils
{
	internal class ThreadSafeCounter
	{
		#region Fields

		private long _counter;

		#endregion

		#region Instance Properties

		public long Value
		{
			get { return Interlocked.Read(ref _counter); }
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
			Interlocked.Decrement(ref _counter);
		}

		private void Increment()
		{
			Interlocked.Increment(ref _counter);
		}

		#endregion

		#region Nested type: ThreadSafeCounterCookie

		internal class ThreadSafeCounterCookie : DisposableBase
		{
			#region Readonly & Static Fields

			private readonly ThreadSafeCounter _threadSafeCounter;

			#endregion

			#region Constructors

			public ThreadSafeCounterCookie(ThreadSafeCounter threadSafeCounter, CrawlerQueueEntry crawlerQueueEntry)
			{
				_threadSafeCounter = threadSafeCounter;
				CrawlerQueueEntry = crawlerQueueEntry;
			}

			#endregion

			#region Instance Methods

			public CrawlerQueueEntry CrawlerQueueEntry { get; private set; }

			protected override void Cleanup()
			{
				_threadSafeCounter.Decrement();
			}

			#endregion
		}

		#endregion
	}
}