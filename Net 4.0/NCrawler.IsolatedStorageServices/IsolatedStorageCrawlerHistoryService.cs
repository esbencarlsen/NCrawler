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

		private readonly Uri m_BaseUri;
		private readonly DictionaryCache m_DictionaryCache = new DictionaryCache(500);
		private readonly bool m_Resume;
		private readonly IsolatedStorageFile m_Store = IsolatedStorageFile.GetMachineStoreForDomain();

		#endregion

		#region Fields

		private long? m_Count;

		#endregion

		#region Constructors

		public IsolatedStorageCrawlerHistoryService(Uri baseUri, bool resume)
		{
			m_BaseUri = baseUri;
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

		#region Instance Properties

		private string WorkFolderPath
		{
			get
			{
				string workFolderName = m_BaseUri.GetHashCode().ToString();
				return Path.Combine(CrawlHistoryName, workFolderName).Max(20);
			}
		}

		#endregion

		#region Instance Methods

		protected override void Add(string key)
		{
			string path = GetFileName(key, true);
			using (IsolatedStorageFileStream isoFile = new IsolatedStorageFileStream(path, FileMode.Create, m_Store))
			{
				using (StreamWriter sw = new StreamWriter(isoFile))
				{
					sw.Write(key);
				}
			}

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
			m_Store.Dispose();
			base.Cleanup();
		}

		protected override bool Exists(string key)
		{
			return AspectF.Define.
				Cache<bool>(m_DictionaryCache, key).
				Return(() =>
					{
						string path = GetFileName(key, false) + "*";
						string[] fileNames = m_Store.GetFileNames(path);
						foreach (string fileName in fileNames)
						{
							using (IsolatedStorageFileStream isoFile = new IsolatedStorageFileStream(Path.Combine(WorkFolderPath, fileName),
								FileMode.Open, FileAccess.Read, m_Store))
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
			return m_Count.HasValue ? m_Count.Value : m_Store.GetFileNames(Path.Combine(WorkFolderPath, "*")).Count();
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
						string[] directoryNames = m_Store.GetDirectoryNames(CrawlHistoryName + "\\*");
						string workFolderName = WorkFolderPath.Split('\\').Last();
						if (directoryNames.Where(w => w == workFolderName).Any())
						{
							m_Store.
								GetFileNames(Path.Combine(WorkFolderPath, "*")).
								ForEach(f => AspectF.Define.
									IgnoreException<IsolatedStorageException>().
									Do(() => m_Store.DeleteFile(Path.Combine(WorkFolderPath, f))));
							m_Store.DeleteDirectory(WorkFolderPath);
						}
					});
			Initialize();
		}

		private void Initialize()
		{
			if (!m_Store.DirectoryExists(CrawlHistoryName))
			{
				m_Store.CreateDirectory(CrawlHistoryName);
			}

			if (!m_Store.DirectoryExists(WorkFolderPath))
			{
				m_Store.CreateDirectory(WorkFolderPath);
			}
		}

		#endregion
	}
}