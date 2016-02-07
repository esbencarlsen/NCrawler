using System;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.RedisServices
{
	public class RedisServicesModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool _resume = true;

		#endregion

		#region Constructors

		public RedisServicesModule(bool resume)
		{
			_resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register((c, p) => new RedisHistoryService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register((c, p) => new RedisQueueService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerQueue>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void Setup(bool resume)
		{
			Setup(new RedisServicesModule(resume));
		}

		#endregion
	}
}