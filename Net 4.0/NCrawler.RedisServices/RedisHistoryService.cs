using System;

using NCrawler.Utils;

using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

namespace NCrawler.RedisServices
{
	public class RedisHistoryService : HistoryServiceBase
	{
		#region Readonly & Static Fields

		private readonly IRedisSet<string> _history;
		private readonly IRedisTypedClient<string> _redis;

		#endregion

		#region Constructors

		public RedisHistoryService(Uri baseUri, bool resume)
		{
			using (RedisClient redisClient = new RedisClient())
			{
				_redis = redisClient.GetTypedClient<string>();
				_history = _redis.Sets[string.Format("barcodes:{0}:history", baseUri)];
				if (!resume)
				{
					_history.Clear();
				}
			}
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			_history.Add(key);
		}

		protected override bool Exists(string key)
		{
			return _history.Contains(key);
		}

		protected override long GetRegisteredCount()
		{
			return _history.Count;
		}

		#endregion
	}
}