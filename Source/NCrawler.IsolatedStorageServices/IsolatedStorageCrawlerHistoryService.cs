using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.IsolatedStorageServices
{
	public class IsolatedStorageCrawlerHistoryService : HistoryServiceBase
	{
		#region Constants

		private const string CrawlHistoryName = @"NCrawlHistory";

		#endregion

		#region Readonly & Static Fields

		private readonly Uri _baseUri;
		private readonly DictionaryCache _dictionaryCache = new DictionaryCache(500);
		private readonly bool _resume;
		private readonly IsolatedStorageFile _store = IsolatedStorageFile.GetMachineStoreForDomain();

		#endregion

		#region Fields

		private long? _count;

		#endregion

		#region Constructors

		public IsolatedStorageCrawlerHistoryService(Uri baseUri, bool resume)
		{
			_baseUri = baseUri;
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

		#region Instance Properties

		private string WorkFolderPath
		{
			get
			{
				string workFolderName = _baseUri.GetHashCode().ToString();
				return Path.Combine(CrawlHistoryName, workFolderName).Max(20);
			}
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			string path = GetFileName(key, true);
			using (IsolatedStorageFileStream isoFile = new IsolatedStorageFileStream(path, FileMode.Create, _store))
			{
				using (StreamWriter sw = new StreamWriter(isoFile))
				{
					sw.Write(key);
				}
			}

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
			_store.Dispose();
			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return AspectF.Define.
				Cache<bool>(_dictionaryCache, key).
				Return(() =>
					{
						string path = GetFileName(key, false) + "*";
						string[] fileNames = _store.GetFileNames(path);
						foreach (string fileName in fileNames)
						{
							using (IsolatedStorageFileStream isoFile = new IsolatedStorageFileStream(Path.Combine(WorkFolderPath, fileName),
								FileMode.Open, FileAccess.Read, _store))
							{
								string content = isoFile.ReadToEnd();
								if (content == key)
								{
									return true;
								}
							}
						}

						return false;
					});
		}

		protected override long GetRegisteredCount()
		{
			return _count.HasValue ? _count.Value : _store.GetFileNames(Path.Combine(WorkFolderPath, "*")).Count();
		}

		protected string GetFileName(string key, bool includeGuid)
		{
			string hashString = key.GetHashCode().ToString();
			string fileName = hashString + "_" + (includeGuid ? Guid.NewGuid().ToString() : string.Empty);
			return Path.Combine(WorkFolderPath, fileName);
		}

		private void Clean()
		{
			AspectF.Define.
				IgnoreException<DirectoryNotFoundException>().
				IgnoreException<IsolatedStorageException>().
				Do(() =>
					{
						string[] directoryNames = _store.GetDirectoryNames(CrawlHistoryName + "\\*");
						string workFolderName = WorkFolderPath.Split('\\').Last();
						if (directoryNames.Where(w => w == workFolderName).Any())
						{
							_store.
								GetFileNames(Path.Combine(WorkFolderPath, "*")).
								ForEach(f => AspectF.Define.
									IgnoreException<IsolatedStorageException>().
									Do(() => _store.DeleteFile(Path.Combine(WorkFolderPath, f))));
							_store.DeleteDirectory(WorkFolderPath);
						}
					});
			Initialize();
		}

		private void Initialize()
		{
			if (!_store.DirectoryExists(CrawlHistoryName))
			{
				_store.CreateDirectory(CrawlHistoryName);
			}

			if (!_store.DirectoryExists(WorkFolderPath))
			{
				_store.CreateDirectory(WorkFolderPath);
			}
		}

		#endregion
	}
}