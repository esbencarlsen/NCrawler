using System;
using System.Linq;

using Autofac;
using Autofac.Core.Lifetime;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Services;

namespace NCrawler
{
	public class NCrawlerModule : Module
	{
		#region Constructors

		static NCrawlerModule()
		{
			Setup();
		}

		#endregion

		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			builder.Register(c => new WebDownloaderV2()).As<IWebDownloader>().SingleInstance().ExternallyOwned();
			builder.Register(c => new InMemoryCrawlerHistoryService()).As<ICrawlerHistory>().InstancePerDependency();
			builder.Register(c => new InMemoryCrawlerQueueService()).As<ICrawlerQueue>().InstancePerDependency();
			builder.Register(c => new SystemTraceLoggerService()).As<ILog>().InstancePerDependency();
#if !DOTNET4
			builder.Register(c => new ThreadTaskRunnerService()).As<ITaskRunner>().InstancePerDependency();
#else
			builder.Register(c => new NativeTaskRunnerService()).As<ITaskRunner>().InstancePerDependency();
#endif
			builder.Register((c, p) => new RobotService(p.TypedAs<Uri>(), c.Resolve<IWebDownloader>())).As<IRobot>().InstancePerDependency();
			builder.Register((c, p) => new CrawlerRulesService(p.TypedAs<Crawler>(), c.Resolve<IRobot>(p), p.TypedAs<Uri>())).As<ICrawlerRules>().InstancePerDependency();
		}

		#endregion

		#region Class Properties

		public static IContainer Container { get; private set; }

		#endregion

		#region Class Methods

		public static void Register(Action<ContainerBuilder> registerCallback)
		{
			ContainerBuilder builder = new ContainerBuilder();
			Container.ComponentRegistry.
				Registrations.
				Where(c => !c.Activator.LimitType.IsAssignableFrom(typeof(LifetimeScope))).
				ForEach(c => builder.RegisterComponent(c));
			registerCallback(builder);
			Container = builder.Build();
		}

		public static void Setup()
		{
			Setup(new NCrawlerModule());
		}

		public static void Setup(params Module[] modules)
		{
			ContainerBuilder builder = new ContainerBuilder();
			modules.ForEach(module => builder.RegisterModule(module));
			Container = builder.Build();
		}

		#endregion
	}
}