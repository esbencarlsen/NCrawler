using System;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.Db4oServices
{
	public class Db4oServicesModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool m_Resume;

		#endregion

		#region Constructors

		public Db4oServicesModule(bool resume)
		{
			m_Resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register((c, p) => new Db4oHistoryService(p.TypedAs<Uri>(), m_Resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register((c, p) => new Db4oQueueService(p.TypedAs<Uri>(), m_Resume)).As
				<ICrawlerQueue>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void Setup(bool resume)
		{
			Setup(new Db4oServicesModule(resume));
		}

		#endregion
	}
}