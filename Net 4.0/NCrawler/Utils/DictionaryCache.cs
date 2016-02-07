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

		private readonly Dictionary<string, object> m_Cache = new Dictionary<string, object>();

		private readonly ReaderWriterLockSlim m_CacheLock =
			new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		private readonly int m_MaxEntries;

		#endregion

		#region Constructors

		public DictionaryCache(int maxEntries)
		{
			m_MaxEntries = maxEntries;
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			m_CacheLock.Dispose();
		}

		#endregion

		#region ICache Members

		public void Add(string key, object value)
		{
			AspectF.Define.
				WriteLock(m_CacheLock).
				Do(() =>
					{
						if (!m_Cache.ContainsKey(key))
						{
							m_Cache.Add(key, value);
						}

						while (m_Cache.Count > m_MaxEntries)
						{
							m_Cache.Remove(m_Cache.Keys.First());
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
				WriteLock(m_CacheLock).
				Do(() => m_Cache[key] = value);
		}

		public void Set(string key, object value, TimeSpan timeout)
		{
			Set(key, value);
		}

		public bool Contains(string key)
		{
			return AspectF.Define.
				ReadLock(m_CacheLock).
				Return(() => m_Cache.ContainsKey(key));
		}

		public void Flush()
		{
			AspectF.Define.
				WriteLock(m_CacheLock).
				Do(() => m_Cache.Clear());
		}

		public object Get(string key)
		{
			return AspectF.Define.
				ReadLock(m_CacheLock).
				Return(() => m_Cache.ContainsKey(key) ? m_Cache[key] : null);
		}

		public void Remove(string key)
		{
			AspectF.Define.
				WriteLock(m_CacheLock).
				Do(() => m_Cache.Remove(key));
		}

		#endregion
	}
}