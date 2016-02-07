using System;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.DbServices
{
	public class DbServicesModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool _resume;

		#endregion

		#region Constructors

		public DbServicesModule(bool resume)
		{
			_resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register((c, p) => new DbCrawlerHistoryService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register((c, p) => new DbCrawlQueueService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerQueue>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void Setup(bool resume)
		{
			Setup(new DbServicesModule(resume));
		}

		#endregion
	}
}