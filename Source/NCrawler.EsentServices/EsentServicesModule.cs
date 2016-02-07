using System;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.EsentServices
{
	public class EsentServicesModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool _resume;

		#endregion

		#region Constructors

		public EsentServicesModule(bool resume)
		{
			_resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register((c, p) => new EsentCrawlerHistoryService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerHistory>().InstancePerDependency();
			builder.Register((c, p) => new EsentCrawlQueueService(p.TypedAs<Uri>(), _resume)).As
				<ICrawlerQueue>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void Setup(bool resume)
		{
			Setup(new EsentServicesModule(resume));
		}

		#endregion
	}
}