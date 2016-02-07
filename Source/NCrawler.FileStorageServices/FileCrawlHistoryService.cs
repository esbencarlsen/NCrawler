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

		private readonly DictionaryCache _dictionaryCache = new DictionaryCache(500);
		private readonly string _storagePath;
		private readonly bool _resume;

		#endregion

		#region Fields

		private long? _count;

		#endregion

		#region Constructors

		public FileCrawlHistoryService(string storagePath, bool resume)
		{
			_storagePath = storagePath;
			_resume = resume;

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
			string path = Path.Combine(_storagePath, GetFileName(key, true));
			File.WriteAllText(path, key);
			_dictionaryCache.Remove(key);
			_count = null;
		}

		protected override void Cleanup()
		{
			if (!_resume)
			{
				Clean();
			}

			_dictionaryCache.Dispose();
			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return AspectF.Define.
				Cache<bool>(_dictionaryCache, key).
				Return(() =>
					{
#if DOTNET4
						IEnumerable<string> fileNames = Directory.EnumerateFiles(_StoragePath, GetFileName(key, false) + "*");
						return fileNames.Select(File.ReadAllText).Any(content => content == key);
#else
					string[] fileNames = Directory.GetFiles(_storagePath, GetFileName(key, false) + "*");
					return fileNames.Select(fileName => File.ReadAllText(fileName)).Any(content => content == key);
#endif
					});
		}

		protected override long GetRegisteredCount()
		{
			if (_count.HasValue)
			{
				return _count.Value;
			}

#if !DOTNET4
			_count = Directory.GetFiles(_storagePath).Count();
#else
			_Count = Directory.EnumerateFiles(_StoragePath).Count();
#endif
			return _count.Value;
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
				Do(() => Directory.Delete(_storagePath, true));

			Initialize();
		}

		private void Initialize()
		{
			if (!Directory.Exists(_storagePath))
			{
				Directory.CreateDirectory(_storagePath);
			}
		}

		#endregion
	}
}