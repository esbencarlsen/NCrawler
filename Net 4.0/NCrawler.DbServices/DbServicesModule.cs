using System;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.DbServices
{
	public class DbServicesModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool m_Resume;

		#endregion

		#region Constructors

		public DbServicesModule(bool resume)
		{
			m_Resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register((c, p) => new DbCrawlerHistoryService(p.TypedAs<Uri>(), m_Resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register((c, p) => new DbCrawlQueueService(p.TypedAs<Uri>(), m_Resume)).As
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