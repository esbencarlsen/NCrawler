using System.IO;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.FileStorageServices
{
	public class FileStorageModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool _resume;
		private readonly string _storagePath;

		#endregion

		#region Constructors

		public FileStorageModule(string storagePath, bool resume)
		{
			_storagePath = storagePath;
			_resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register(c => new FileCrawlHistoryService(Path.Combine(_storagePath, "NCrawlHistory"), _resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register(c => new FileCrawlQueueService(Path.Combine(_storagePath, "NCrawlQueue"), _resume)).As
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