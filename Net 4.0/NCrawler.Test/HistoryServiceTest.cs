using System;
using System.IO;
using System.Reflection;

using NCrawler.Db4oServices;
using NCrawler.DbServices;
using NCrawler.EsentServices;
using NCrawler.FileStorageServices;
using NCrawler.Interfaces;
using NCrawler.IsolatedStorageServices;
using NCrawler.Services;
using NCrawler.Test.Helpers;

using NUnit.Framework;

namespace NCrawler.Test
{
	[TestFixture]
	public class HistoryServiceTest
	{
		public void Test1(ICrawlerHistory crawlerHistory)
		{
			Assert.NotNull(crawlerHistory);
			Assert.AreEqual(0, crawlerHistory.RegisteredCount);

			if (crawlerHistory is IDisposable)
			{
				((IDisposable)crawlerHistory).Dispose();
			}
		}

		public void Test2(ICrawlerHistory crawlerHistory)
		{
			Assert.NotNull(crawlerHistory);
			crawlerHistory.Register("123");
			Assert.AreEqual(1, crawlerHistory.RegisteredCount);

			if (crawlerHistory is IDisposable)
			{
				((IDisposable)crawlerHistory).Dispose();
			}
		}

		public void Test3(ICrawlerHistory crawlerHistory)
		{
			Assert.NotNull(crawlerHistory);
			Assert.IsTrue(crawlerHistory.Register("123"));
			Assert.IsFalse(crawlerHistory.Register("123"));

			if (crawlerHistory is IDisposable)
			{
				((IDisposable)crawlerHistory).Dispose();
			}
		}

		public void Test4(ICrawlerHistory crawlerHistory)
		{
			Assert.NotNull(crawlerHistory);
			Assert.IsTrue(crawlerHistory.Register("123"));
			Assert.IsTrue(crawlerHistory.Register("1234"));

			if (crawlerHistory is IDisposable)
			{
				((IDisposable)crawlerHistory).Dispose();
			}
		}

		public void Test5(ICrawlerHistory crawlerHistory)
		{
			Assert.NotNull(crawlerHistory);

			for (int i = 0; i < 10; i++)
			{
				crawlerHistory.Register(i.ToString());
			}

			for (int i = 0; i < 10; i++)
			{
				Assert.IsFalse(crawlerHistory.Register(i.ToString()));
			}

			for (int i = 10; i < 20; i++)
			{
				Assert.IsTrue(crawlerHistory.Register(i.ToString()));
			}

			if (crawlerHistory is IDisposable)
			{
				((IDisposable)crawlerHistory).Dispose();
			}
		}

		public void Test6(ICrawlerHistory crawlerHistory)
		{
			Assert.NotNull(crawlerHistory);

			int count = 0;
			foreach (string url in new StringPatternGenerator("http://ncrawler[a,b,c,d,e,f].codeplex.com/view[0-10].aspx?param1=[a-c]&param2=[D-F]"))
			{
				Assert.IsTrue(crawlerHistory.Register(url));
				Assert.IsFalse(crawlerHistory.Register(url));
				count++;
				Assert.AreEqual(count, crawlerHistory.RegisteredCount);
			}

			if (crawlerHistory is IDisposable)
			{
				((IDisposable)crawlerHistory).Dispose();
			}
		}

		public void RunCrawlHistoryTests(Func<ICrawlerHistory> getCrawlerHistoryService)
		{
			Test1(getCrawlerHistoryService());
			Test2(getCrawlerHistoryService());
			Test3(getCrawlerHistoryService());
			Test4(getCrawlerHistoryService());
			Test5(getCrawlerHistoryService());
			Test6(getCrawlerHistoryService());
		}

		[Test]
		public void TestHistoryService()
		{
			RunCrawlHistoryTests(() => new InMemoryCrawlerHistoryService());
			RunCrawlHistoryTests(() => new Db4oHistoryService(new Uri("http://www.ncrawler.com"), false));
			RunCrawlHistoryTests(() => new EsentCrawlerHistoryService(new Uri("http://www.ncrawler.com"), false));
			RunCrawlHistoryTests(() => new FileCrawlHistoryService(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName + "\\NCrawlerUnitTest", false));
			RunCrawlHistoryTests(() => new IsolatedStorageCrawlerHistoryService(new Uri("http://www.ncrawler.com"), false));
			//RunCrawlHistoryTests(() => new DbCrawlerHistoryService(new Uri("http://www.ncrawler.com"), false));
		}
	}
}