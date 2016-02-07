using System;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.Db4oServices
{
	public class Db4OServicesModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool _resume;

		#endregion

		#region Constructors

		public Db4OServicesModule(bool resume)
		{
			_resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register((c, p) => new Db4OHistoryService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register((c, p) => new Db4OQueueService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerQueue>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void Setup(bool resume)
		{
			Setup(new Db4OServicesModule(resume));
		}

		#endregion
	}
}