using System.IO;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.FileStorageServices
{
	public class FileStorageModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool m_Resume;
		private readonly string m_StoragePath;

		#endregion

		#region Constructors

		public FileStorageModule(string storagePath, bool resume)
		{
			m_StoragePath = storagePath;
			m_Resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register(c => new FileCrawlHistoryService(Path.Combine(m_StoragePath, "NCrawlHistory"), m_Resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register(c => new FileCrawlQueueService(Path.Combine(m_StoragePath, "NCrawlQueue"), m_Resume)).As
				<ICrawlerQueue>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void Setup(string storagePath, bool resume)
		{
			Setup(new FileStorageModule(storagePath, resume));
		}

		#endregion
	}
}