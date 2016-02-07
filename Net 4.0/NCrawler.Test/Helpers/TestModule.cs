using System.IO;
using System.Reflection;

using Autofac;

using NCrawler.Db4oServices;
using NCrawler.DbServices;
using NCrawler.EsentServices;
using NCrawler.FileStorageServices;
using NCrawler.Interfaces;
using NCrawler.IsolatedStorageServices;

using Module = Autofac.Module;

namespace NCrawler.Test.Helpers
{
	public class TestModule : Module
	{
		#region Instance Methods

		protected override void Load(ContainerBuilder builder)
		{
			builder.Register(c => new FakeLoggerService()).As<ILog>().InstancePerDependency();
			builder.Register(c => new FakeDownloader()).As<IWebDownloader>().InstancePerDependency();
		}

		#endregion

		#region Class Methods

		public static void SetupDb4oServicesStorage()
		{
			NCrawlerModule.Setup(new Db4oServicesModule(false), new TestModule());
		}

		public static void SetupDbServicesStorage()
		{
			NCrawlerModule.Setup(new DbServicesModule(false), new TestModule());
		}

		public static void SetupESentServicesStorage()
		{
		    NCrawlerModule.Setup(new EsentServicesModule(false), new TestModule());
		}

		public static void SetupFileStorage()
		{
			string storagePath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
			NCrawlerModule.Setup(new FileStorageModule(storagePath, false), new TestModule());
		}

		public static void SetupInMemoryStorage()
		{
			NCrawlerModule.Setup(new NCrawlerModule(), new TestModule());
		}

		public static void SetupIsolatedStorage()
		{
			NCrawlerModule.Setup(new IsolatedStorageModule(false), new TestModule());
		}

		public static void SetupSqLiteStorage()
		{
			//NCrawlerModule.Setup(new SqLiteServicesModule(false), new TestModule());
		}

		#endregion
	}
}