using System;
using System.Collections.Generic;

using NCrawler.Interfaces;

using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

namespace NCrawler.RedisServices
{
	public class RedisQueueService : ICrawlerQueue
	{
		#region Readonly & Static Fields

		private readonly IRedisList<Entry> _queue;
		private readonly IRedisTypedClient<Entry> _redis;

		#endregion

		#region Constructors

		public RedisQueueService(Uri baseUri, bool resume)
		{
			using (RedisClient redisClient = new RedisClient())
			{
				_redis = redisClient.GetTypedClient<Entry>();
				_queue = _redis.Lists[string.Format("barcodes:{0}:queue", baseUri)];
				if (!resume)
				{
					_queue.Clear();
				}
			}
		}

		#endregion

		#region ICrawlerQueue Members

		public long Count
		{
			get { return _queue.Count; }
		}

		public CrawlerQueueEntry Pop()
		{
			Entry qt = _queue.Pop();
			CrawlerQueueEntry obj = null;
			if (qt != null)
			{
				obj = qt.ToCrawlerQueueEntry();
			}

			return obj;
		}

		public void Push(CrawlerQueueEntry crawlerQueueEntry)
		{
			_queue.Add(Entry.FromCrawlerQueueEntry(crawlerQueueEntry));
		}

		#endregion
	}

	[Serializable]
	public class Entry
	{
		#region Fields

		private Dictionary<string, object> Properties = new Dictionary<string, object>();

		#endregion

		#region Instance Properties

		public int ReferrerDepth { get; set; }
		public string ReferrerUri { get; set; }
		public int StepDepth { get; set; }
		public string StepUri { get; set; }

		#endregion

		#region Instance Methods

		public CrawlerQueueEntry ToCrawlerQueueEntry()
		{
			CrawlerQueueEntry ce = new CrawlerQueueEntry
				{
					Properties = Properties,
					CrawlStep = new CrawlStep(new Uri(StepUri), StepDepth)
				};
			if (ReferrerUri != null)
			{
				ce.Referrer = new CrawlStep(new Uri(ReferrerUri), ReferrerDepth);
			}

			return ce;
		}

		#endregion

		#region Class Methods

		public static Entry FromCrawlerQueueEntry(CrawlerQueueEntry ce)
		{
			Entry e = new Entry
				{
					StepUri = ce.CrawlStep.Uri.ToString(),
					StepDepth = ce.CrawlStep.Depth,
					Properties = ce.Properties
				};
			if (ce.Referrer != null)
			{
				e.ReferrerUri = ce.Referrer.Uri.ToString();
				e.ReferrerDepth = ce.Referrer.Depth;
			}

			return e;
		}

		#endregion
	}
}