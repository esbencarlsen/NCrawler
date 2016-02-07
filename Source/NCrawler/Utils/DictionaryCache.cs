using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.Utils
{
	public class DictionaryCache : DisposableBase, ICache
	{
		#region Readonly & Static Fields

		private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

		private readonly ReaderWriterLockSlim _cacheLock =
			new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		private readonly int _maxEntries;

		#endregion

		#region Constructors

		public DictionaryCache(int maxEntries)
		{
			_maxEntries = maxEntries;
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			_cacheLock.Dispose();
		}

		#endregion

		#region ICache Members

		public void Add(string key, object value)
		{
			AspectF.Define.
				WriteLock(_cacheLock).
				Do(() =>
					{
						if (!_cache.ContainsKey(key))
						{
							_cache.Add(key, value);
						}

						while (_cache.Count > _maxEntries)
						{
							_cache.Remove(_cache.Keys.First());
						}
					});
		}

		public void Add(string key, object value, TimeSpan timeout)
		{
			Add(key, value);
		}

		public void Set(string key, object value)
		{
			AspectF.Define.
				WriteLock(_cacheLock).
				Do(() => _cache[key] = value);
		}

		public void Set(string key, object value, TimeSpan timeout)
		{
			Set(key, value);
		}

		public bool Contains(string key)
		{
			return AspectF.Define.
				ReadLock(_cacheLock).
				Return(() => _cache.ContainsKey(key));
		}

		public void Flush()
		{
			AspectF.Define.
				WriteLock(_cacheLock).
				Do(() => _cache.Clear());
		}

		public object Get(string key)
		{
			return AspectF.Define.
				ReadLock(_cacheLock).
				Return(() => _cache.ContainsKey(key) ? _cache[key] : null);
		}

		public void Remove(string key)
		{
			AspectF.Define.
				WriteLock(_cacheLock).
				Do(() => _cache.Remove(key));
		}

		#endregion
	}
}