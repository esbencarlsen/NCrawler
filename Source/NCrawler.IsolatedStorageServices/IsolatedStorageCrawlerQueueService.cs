using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.IsolatedStorageServices
{
	public class IsolatedStorageCrawlerQueueService : CrawlerQueueServiceBase
	{
		#region Constants

		private const string NCrawlerQueueDirectoryName = "NCrawler";

		#endregion

		#region Readonly & Static Fields

		private readonly Uri _crawlStart;
		private readonly IsolatedStorageFile _store;

		#endregion

		#region Fields

		private long _count;

		#endregion

		#region Constructors

		public IsolatedStorageCrawlerQueueService(Uri crawlStart, bool resume)
		{
			_crawlStart = crawlStart;
			_store = IsolatedStorageFile.GetMachineStoreForDomain();

			if (!resume)
			{
				Clean();
			}
			else
			{
				Initialize();
				_count = _store.GetFileNames(Path.Combine(WorkFolderPath, "*")).Count();
			}
		}

		#endregion

		#region Instance Properties

		private string WorkFolderPath
		{
			get
			{
				string workFolderName = _crawlStart.GetHashCode().ToString();
				return Path.Combine(NCrawlerQueueDirectoryName, workFolderName).Max(20);
			}
		}

		#endregion

		#region Instance Methods

		protected override void Cleanup()
		{
			Clean();
			_store.Dispose();
			base.Cleanup();
		}

		protected override long GetCount()
		{
			return Interlocked.Read(ref _count);
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			string fileName = _store.GetFileNames(Path.Combine(WorkFolderPath, "*")).FirstOrDefault();
			if (fileName.IsNullOrEmpty())
			{
				return null;
			}

			string path = Path.Combine(WorkFolderPath, fileName);
			try
			{
				using (IsolatedStorageFileStream isoFile =
					new IsolatedStorageFileStream(path, FileMode.Open, _store))
				{
					return isoFile.FromBinary<CrawlerQueueEntry>();
				}
			}
			finally
			{
				_store.DeleteFile(path);
				Interlocked.Decrement(ref _count);
			}
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			byte[] data = crawlerQueueEntry.ToBinary();
			string path = Path.Combine(WorkFolderPath, Guid.NewGuid().ToString());
			using (IsolatedStorageFileStream isoFile = new IsolatedStorageFileStream(path, FileMode.Create, _store))
			{
				isoFile.Write(data, 0, data.Length);
			}

			Interlocked.Increment(ref _count);
		}

		protected void Clean()
		{
			AspectF.Define.
				IgnoreExceptions().
				Do(() =>
					{
						string[] directoryNames = _store.GetDirectoryNames(NCrawlerQueueDirectoryName + "\\*");
						string workFolderName = WorkFolderPath.Split('\\').Last();
						if (directoryNames.Where(w => w == workFolderName).Any())
						{
							_store.
								GetFileNames(Path.Combine(WorkFolderPath, "*")).
								ForEach(f => _store.DeleteFile(Path.Combine(WorkFolderPath, f)));
							_store.DeleteDirectory(WorkFolderPath);
						}
					});
			Initialize();
		}

		/// <summary>
		/// 	Initialize crawler queue
		/// </summary>
		private void Initialize()
		{
			if (!_store.DirectoryExists(NCrawlerQueueDirectoryName))
			{
				_store.CreateDirectory(NCrawlerQueueDirectoryName);
			}

			if (!_store.DirectoryExists(WorkFolderPath))
			{
				_store.CreateDirectory(WorkFolderPath);
			}
		}

		#endregion
	}
}