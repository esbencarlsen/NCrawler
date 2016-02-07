using System;
using System.IO;
using System.Linq;
using System.Threading;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.FileStorageServices
{
	public class FileCrawlQueueService : CrawlerQueueServiceBase
	{
		#region Readonly & Static Fields

		private readonly string _storagePath;

		#endregion

		#region Fields

		private long _count;

		#endregion

		#region Constructors

		public FileCrawlQueueService(string storagePath, bool resume)
		{
			_storagePath = storagePath;

			if (!resume)
			{
				Clean();
			}
			else
			{
				Initialize();
				_count = Directory.GetFiles(_storagePath).Count();
			}
		}

		#endregion

		#region Instance Methods

		protected override long GetCount()
		{
			return Interlocked.Read(ref _count);
		}

		protected override CrawlerQueueEntry PopImpl()
		{
#if !DOTNET4
			string fileName = Directory.GetFiles(_storagePath).FirstOrDefault();
#else
			string fileName = Directory.EnumerateFiles(_StoragePath).FirstOrDefault();
#endif
			if (fileName.IsNullOrEmpty())
			{
				return null;
			}

			try
			{
				return File.ReadAllBytes(fileName).FromBinary<CrawlerQueueEntry>();
			}
			finally
			{
				File.Delete(fileName);
				Interlocked.Decrement(ref _count);
			}
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			byte[] data = crawlerQueueEntry.ToBinary();
			string fileName = Path.Combine(_storagePath, Guid.NewGuid().ToString());
			File.WriteAllBytes(fileName, data);
			Interlocked.Increment(ref _count);
		}

		protected void Clean()
		{
			AspectF.Define.
				IgnoreException<DirectoryNotFoundException>().
				Do(() => Directory.Delete(_storagePath, true));

			Initialize();
		}

		/// <summary>
		/// 	Initialize crawler queue
		/// </summary>
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