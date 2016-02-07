using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.FileStorageServices
{
	public class FileCrawlHistoryService : HistoryServiceBase
	{
		#region Readonly & Static Fields

		private readonly DictionaryCache m_DictionaryCache = new DictionaryCache(500);
		private readonly string m_StoragePath;
		private readonly bool m_Resume;

		#endregion

		#region Fields

		private long? m_Count;

		#endregion

		#region Constructors

		public FileCrawlHistoryService(string storagePath, bool resume)
		{
			m_StoragePath = storagePath;
			m_Resume = resume;

			if (!resume)
			{
				Clean();
			}
			else
			{
				Initialize();
			}
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			string path = Path.Combine(m_StoragePath, GetFileName(key, true));
			File.WriteAllText(path, key);
			m_DictionaryCache.Remove(key);
			m_Count = null;
		}

		protected override void Cleanup()
		{
			if (!m_Resume)
			{
				Clean();
			}

			m_DictionaryCache.Dispose();
			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return AspectF.Define.
				Cache<bool>(m_DictionaryCache, key).
				Return(() =>
					{
#if DOTNET4
						IEnumerable<string> fileNames = Directory.EnumerateFiles(m_StoragePath, GetFileName(key, false) + "*");
						return fileNames.Select(File.ReadAllText).Any(content => content == key);
#else
					string[] fileNames = Directory.GetFiles(m_StoragePath, GetFileName(key, false) + "*");
					return fileNames.Select(fileName => File.ReadAllText(fileName)).Any(content => content == key);
#endif
					});
		}

		protected override long GetRegisteredCount()
		{
			if (m_Count.HasValue)
			{
				return m_Count.Value;
			}

#if !DOTNET4
			m_Count = Directory.GetFiles(m_StoragePath).Count();
#else
			m_Count = Directory.EnumerateFiles(m_StoragePath).Count();
#endif
			return m_Count.Value;
		}

		protected string GetFileName(string key, bool includeGuid)
		{
			string hashString = key.GetHashCode().ToString();
			return hashString + "_" + (includeGuid ? Guid.NewGuid().ToString() : string.Empty);
		}

		private void Clean()
		{
			AspectF.Define.
				IgnoreException<DirectoryNotFoundException>().
				Do(() => Directory.Delete(m_StoragePath, true));

			Initialize();
		}

		private void Initialize()
		{
			if (!Directory.Exists(m_StoragePath))
			{
				Directory.CreateDirectory(m_StoragePath);
			}
		}

		#endregion
	}
}