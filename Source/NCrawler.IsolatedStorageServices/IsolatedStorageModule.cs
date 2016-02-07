using System;

using Autofac;

using NCrawler.Interfaces;

namespace NCrawler.IsolatedStorageServices
{
	public class IsolatedStorageModule : NCrawlerModule
	{
		#region Readonly & Static Fields

		private readonly bool _resume;

		#endregion

		#region Constructors

		public IsolatedStorageModule(bool resume)
		{
			_resume = resume;
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);

			builder.Register((c, p) =>
				{
					Uri crawlStart = p.TypedAs<Uri>();
					return new IsolatedStorageCrawlerHistoryService(crawlStart, _resume);
				}).As<ICrawlerHistory>().InstancePerDependency();

			builder.Register((c, p) =>
				{
					Uri crawlStart = p.TypedAs<Uri>();
					return new IsolatedStorageCrawlerQueueService(crawlStart, _resume);
				}).As<ICrawlerQueue>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void Setup(bool resume)
		{
			Setup(new IsolatedStorageModule(resume));
		}

		#endregion
	}
}