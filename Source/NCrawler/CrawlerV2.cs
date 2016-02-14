//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Threading.Tasks.Dataflow;

//using NCrawler.Interfaces;

//using Serilog;

//namespace NCrawler
//{
//	public class CrawlerV22 : ICrawler
//	{
//		private readonly CrawlerConfiguration _crawlerConfiguration;
//		private readonly ILogger _logger;
//		private readonly IWebDownloaderV2 _webDownloader;
//		//public static void Crawl()
//		//{
//		//	ICrawler crawler = new CrawlerConfiguration()
//		//		.Crawl("http://www.vergic.com")
//		//		.Crawl("http://www.codeplex.com")
//		//		.AddPipelineStep(null)
//		//		.CreateCrawler();
//		//	using (crawler.Crawl().Subscribe(IObserver<.>))
//		//	{

//		//	}
//		//}
//		public CrawlerV22(
//			CrawlerConfiguration crawlerConfiguration,
//			ILogger logger,
//			IWebDownloaderV2 webDownloader)
//		{
//			_crawlerConfiguration = crawlerConfiguration;
//			_logger = logger;
//			_webDownloader = webDownloader;
//		}

//		public Task Crawl()
//		{
//			HashSet<string> linksAlreadyFound = new HashSet<string>();
//			long crawlCount = 0;
//			TransformBlock<Tuple<CrawlStep, CrawlStep>, Tuple<CrawlStep, CrawlStep>> filterBlock =
//				new TransformBlock<Tuple<CrawlStep, CrawlStep>, Tuple<CrawlStep, CrawlStep>>(crawlStep =>
//				{
//					if (_crawlerConfiguration.MaximumCrawlDepth.HasValue
//						&& _crawlerConfiguration.MaximumCrawlDepth.Value > crawlStep.Item1.Depth)
//					{
//						_logger.Verbose("MaximumCrawlDepth exceeded", crawlStep.Item1.Depth);
//						return null;
//					}

//					if (_crawlerConfiguration.MaximumCrawlCount.HasValue
//						&& _crawlerConfiguration.MaximumCrawlCount.Value > crawlCount)
//					{
//						_logger.Verbose("MaximumCrawlCount exceeded", crawlCount);
//						return null;
//					}

//					if (!linksAlreadyFound.Add(
//						crawlStep.Item1.Uri.GetComponents(_crawlerConfiguration.UriSensitivity, UriFormat.Unescaped).ToString()))
//					{
//						return null;
//					}

//					if (_crawlerConfiguration.IncludeFilter.Any(filter => filter.Match(crawlStep.Item1.Uri, crawlStep.Item2)))
//					{
//						_logger.Verbose("Whitelisted {requestUrl}", crawlStep.Item1.Uri);
//						crawlCount++;
//						return crawlStep;
//					}

//					if (_crawlerConfiguration.ExcludeFilter.Any(filter => filter.Match(crawlStep.Item1.Uri, crawlStep.Item2)))
//					{
//						_logger.Verbose("Blacklisted {requestUrl}", crawlStep.Item1.Uri);
//						return null;
//					}

//					crawlCount++;
//					return crawlStep;
//				}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1});

//			TransformBlock<Tuple<CrawlStep, CrawlStep>, PropertyBag> downloadBlock =
//				new TransformBlock<Tuple<CrawlStep, CrawlStep>, PropertyBag>(async crawlStep =>
//				{
//					_logger.Verbose("Downloading {requestUrl}", crawlStep.Item1.Uri);
//					PropertyBag propertyBag = await _webDownloader.Download(crawlStep.Item1, crawlStep.Item2);
//					return propertyBag;
//				}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = _crawlerConfiguration.MaxDegreeOfParallelism});

//			ActionBlock<PropertyBag> pipelineBlock = new ActionBlock<PropertyBag>(propertyBag =>
//			{
//				if (propertyBag != null)
//				{
//					foreach (IPipelineStep pipelineStep in _crawlerConfiguration.Pipeline)
//					{
//						pipelineStep.Process(null, propertyBag);
//					}
//				}

//				if (filterBlock.InputCount == 0
//					&& !filterBlock.Completion.IsCompleted
//					&& !filterBlock.Completion.IsCanceled
//					&& !filterBlock.Completion.IsFaulted)
//				{
//					filterBlock.Complete();
//				}
//			}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1});

//			filterBlock.LinkTo(downloadBlock, new DataflowLinkOptions {PropagateCompletion = true},
//				crawlStep => crawlStep != null);
//			downloadBlock.LinkTo(pipelineBlock, new DataflowLinkOptions {PropagateCompletion = true},
//				propertyBag => propertyBag != null);
//			foreach (Uri startUri in _crawlerConfiguration.StartUris)
//			{
//				filterBlock.Post(new Tuple<CrawlStep, CrawlStep>(new CrawlStep(startUri, 0), null));
//			}

//			return Task.WhenAll(downloadBlock.Completion, downloadBlock.Completion);
//		}
//	}
//}