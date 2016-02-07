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

		private readonly string m_StoragePath;

		#endregion

		#region Fields

		private long m_Count;

		#endregion

		#region Constructors

		public FileCrawlQueueService(string storagePath, bool resume)
		{
			m_StoragePath = storagePath;

			if (!resume)
			{
				Clean();
			}
			else
			{
				Initialize();
				m_Count = Directory.GetFiles(m_StoragePath).Count();
			}
		}

		#endregion

		#region Instance Methods

		protected override long GetCount()
		{
			return Interlocked.Read(ref m_Count);
		}

		protected override CrawlerQueueEntry PopImpl()
		{
#if !DOTNET4
			string fileName = Directory.GetFiles(m_StoragePath).FirstOrDefault();
#else
			string fileName = Directory.EnumerateFiles(m_StoragePath).FirstOrDefault();
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
				Interlocked.Decrement(ref m_Count);
			}
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			byte[] data = crawlerQueueEntry.ToBinary();
			string fileName = Path.Combine(m_StoragePath, Guid.NewGuid().ToString());
			File.WriteAllBytes(fileName, data);
			Interlocked.Increment(ref m_Count);
		}

		protected void Clean()
		{
			AspectF.Define.
				IgnoreException<DirectoryNotFoundException>().
				Do(() => Directory.Delete(m_StoragePath, true));

			Initialize();
		}

		/// <summary>
		/// 	Initialize crawler queue
		/// </summary>
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