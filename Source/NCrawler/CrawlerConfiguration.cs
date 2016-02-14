using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Pipeline;
using NCrawler.Utils;

using Serilog;

namespace NCrawler
{
	public class CrawlerConfiguration
	{
		public ILogger Logger { get; set; }
		internal readonly List<IPipelineStep> Pipeline = new List<IPipelineStep>();
		internal readonly List<Uri> StartUris = new List<Uri>();

		public CrawlerConfiguration()
		{
			RemoveDotNetServicePointRestrictions();
			SetDefaultServicePointManagerSettings();

			Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.ColoredConsole().CreateLogger();
		}

		/// <summary>
		///     Maximum amount of time allowed to make a connection
		/// </summary>
		public TimeSpan? ConnectionReadTimeout { get; set; }

		/// <summary>
		///     In seconds
		/// </summary>
		public TimeSpan? ConnectionTimeout { get; set; }

		/// <summary>
		///     Maximum size a single download is allowed to be
		/// </summary>
		public uint? MaximumContentSize { get; set; }

		public uint DownloadBufferSize { get; set; }

		/// <summary>
		///     Maximum number of steps to download before ending crawl
		/// </summary>
		public int? MaximumCrawlCount { get; set; }

		/// <summary>
		///     Maximum download error allowed before crawl is cancelled
		/// </summary>
		public int? MaximumHttpDownloadErrors { get; set; }

		/// <summary>
		///     Number of crawler threads to use
		/// </summary>
		public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

		/// <summary>
		///     The maximum amount of time the crawler is allowed to run
		/// </summary>
		public TimeSpan? MaximumCrawlTime { get; set; }

		/// <summary>
		///     How many times crawler should try to download a single url before giving up
		/// </summary>
		public int? DownloadRetryCount { get; set; }

		/// <summary>
		///     How long the crawler should wait before retrying a download
		/// </summary>
		public TimeSpan? DownloadRetryWaitDuration { get; set; }

		/// <summary>
		///     Use cookies when downloading
		/// </summary>
		public bool UseCookies { get; set; }

		public CrawlerConfiguration Default()
		{
			RemoveDuplicates();
			DownloadStep();
			return this;
		}

		public void RemoveDotNetServicePointRestrictions()
		{
			ServicePointManager.MaxServicePoints = 999999;
			ServicePointManager.DefaultConnectionLimit = 999999;
		}

		public void SetDefaultServicePointManagerSettings()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
			ServicePointManager.CheckCertificateRevocationList = true;
			ServicePointManager.EnableDnsRoundRobin = true;
		}

		public void AddLoggerStep()
		{
			LambdaFilter((crawler, propertyBag) =>
			{
				Logger.Verbose("Uri {uri}", propertyBag.Step.Uri);
				return true;
			});
		}

		public CrawlerConfiguration DownloadStep(int? maxDegreeOfParallelism = null)
		{
			return AddPipelineStep(new DownloadPipelineStep(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
		}

		public CrawlerConfiguration RemoveDuplicates(UriComponents uriSensitivity = UriComponents.AbsoluteUri)
		{
			HashSet<string> linksAlreadyCrawled = new HashSet<string>();
			return LambdaFilter((crawler, propertyBag) =>
				!linksAlreadyCrawled.Add(propertyBag.Step.Uri.GetComponents(uriSensitivity, UriFormat.Unescaped)));
		}

		public CrawlerConfiguration MaxCrawlDepth(int maxCrawlDepth)
		{
			return LambdaFilter((crawler, propertyBag) => propertyBag.Step.Depth < maxCrawlDepth);
		}

		public CrawlerConfiguration MaxCrawlCount(int maxCrawlCount)
		{
			int downloadCounter = 0;
			return LambdaFilter((crawler, propertyBag) => downloadCounter++ < maxCrawlCount);
		}

		public CrawlerConfiguration LogDownloadTime()
		{
			return Do((crawler, propertyBag) =>
			 {
				 Logger.Verbose("{0} downloaded in {1}", propertyBag.Step.Uri, propertyBag.DownloadTime);
			 });
		}

		public CrawlerConfiguration MaximumUrlLength(int maxUrlLength)
		{
			return LambdaFilter((crawler, propertyBag) => propertyBag.Step.Uri.ToString().Length < maxUrlLength);
		}

		public CrawlerConfiguration Do(Action<ICrawler, PropertyBag> predicate, int maxDegreeOfParallelism = 1)
		{
			return AddPipelineStep(new LambdaFilterPipelineStep((crawler, bag) =>
			{
				predicate(crawler, bag);
				return true;
			}, maxDegreeOfParallelism));
		}

		public CrawlerConfiguration LambdaFilter(Func<ICrawler, PropertyBag, bool> predicate, int maxDegreeOfParallelism = 1)
		{
			return AddPipelineStep(new LambdaFilterPipelineStep(predicate, maxDegreeOfParallelism));
		}

		public CrawlerConfiguration LambdaFilter(Func<ICrawler, PropertyBag, Task<bool>> predicate,
			int maxDegreeOfParallelism = 1)
		{
			return AddPipelineStep(new LambdaFilterPipelineStep(predicate, maxDegreeOfParallelism));
		}

		public CrawlerConfiguration RegexFilter(Regex regex)
		{
			return LambdaFilter((crawler, propertyBag) => regex.Match(propertyBag.Step.Uri.ToString()).Success);
		}

		public CrawlerConfiguration ExtractEmail(int? maxDegreeOfParallelism = null)
		{
			return AddPipelineStep(
				new EMailEntityExtractionProcessor(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
		}

		private string _userAgent = "Mozilla";
		public CrawlerConfiguration UserAgent(string userAgent)
		{
			_userAgent = userAgent;
			return this;
		}

		public CrawlerConfiguration LogExceptions()
		{
			return Do((crawler, propertyBag) =>
			{
				if (propertyBag.Exceptions != null)
				{
					foreach (Exception exception in propertyBag.Exceptions)
					{
						LogExceptionHelper(Logger, exception, propertyBag);
					}
				}
			});
		}

		private static void LogExceptionHelper(ILogger logger, Exception exception, PropertyBag propertyBag)
		{
			AggregateException aggregateException = exception as AggregateException;
			if (aggregateException != null)
			{
				foreach (Exception innerAggregateException in aggregateException.InnerExceptions)
				{
					LogExceptionHelper(logger, innerAggregateException, propertyBag);
				}
			}
			else
			{
				logger.Error(exception, "Error while crawling: {0}", propertyBag?.Step?.Uri);
			}
		}

		public CrawlerConfiguration Crawl(string url)
		{
			AspectF.Define.NotNull(url, nameof(url));
			StartUris.Add(new Uri(url));
			return this;
		}

		public CrawlerConfiguration Crawl(Uri uri)
		{
			AspectF.Define.NotNull(uri, nameof(uri));
			StartUris.Add(uri);
			return this;
		}

		public CrawlerConfiguration AddPipelineStep(IPipelineStep pipelineStep)
		{
			AspectF.Define.NotNull(pipelineStep, nameof(pipelineStep));
			Pipeline.Add(pipelineStep);
			return this;
		}

		public CrawlerConfiguration AddPipelineStep<T>() where T : IPipelineStep, new()
		{
			AddPipelineStep(new T());
			return this;
		}

		public Task RunAsync()
		{
			TransformBlock<Uri, PropertyBag> ingestBlock = new TransformBlock<Uri, PropertyBag>(input =>
			{
				PropertyBag result = new PropertyBag
				{
					OriginalUrl = input.ToString(),
					UserAgent = _userAgent,
					Step = new CrawlStep(input, 0)
				};

				return result;
			}, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = MaxDegreeOfParallelism
			});

			TransformBlock<PropertyBag, PropertyBag> ingestBlockForAggregation =
				new TransformBlock<PropertyBag, PropertyBag>(input => input, new ExecutionDataflowBlockOptions
				{
					MaxDegreeOfParallelism = MaxDegreeOfParallelism
				});

			CrawlIngestionHelper crawlIngestionHelper = new CrawlIngestionHelper(ingestBlockForAggregation, _userAgent);
			TransformBlock<PropertyBag, PropertyBag>[] pipeline = Pipeline
				.Select(pipelineStep =>
				{
					return new TransformBlock<PropertyBag, PropertyBag>(async propertyBag =>
					{
						if (propertyBag.StopPipelining)
						{
							return propertyBag;
						}

						try
						{
							propertyBag.StopPipelining = !await pipelineStep.Process(crawlIngestionHelper, propertyBag);
						}
						catch (Exception exception)
						{
							propertyBag.Exceptions.Add(exception);
						}

						return propertyBag;
					}, new ExecutionDataflowBlockOptions
					{
						MaxDegreeOfParallelism = pipelineStep.MaxDegreeOfParallelism
					});
				})
				.ToArray();

			ActionBlock<PropertyBag> terminationCheckerBlock = new ActionBlock<PropertyBag>(propertyBag =>
			{
				if (ingestBlock.InputCount == 0
					&& ingestBlock.OutputCount == 0
					&& !ingestBlock.Completion.IsCompleted
					&& !ingestBlock.Completion.IsCanceled
					&& !ingestBlock.Completion.IsFaulted
					&& ingestBlockForAggregation.InputCount == 0
					&& ingestBlockForAggregation.OutputCount == 0)
				{
					if (pipeline.Any(transformBlock => transformBlock.InputCount != 0 || transformBlock.OutputCount != 0))
					{
						return;
					}

					ingestBlock.Complete();
				}
			}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1});

			ingestBlock.LinkTo(ingestBlockForAggregation, new DataflowLinkOptions {PropagateCompletion = true});
			TransformBlock<PropertyBag, PropertyBag> previous = ingestBlockForAggregation;
			foreach (TransformBlock<PropertyBag, PropertyBag> transformBlock in pipeline)
			{
				previous.LinkTo(transformBlock, new DataflowLinkOptions {PropagateCompletion = true});
				previous = transformBlock;
			}

			previous.LinkTo(terminationCheckerBlock, new DataflowLinkOptions {PropagateCompletion = true});
			foreach (Uri startUri in StartUris)
			{
				ingestBlock.Post(startUri);
			}

			return terminationCheckerBlock.Completion;
		}

		public void Run()
		{
			RunAsync().Wait();
		}

		private class CrawlIngestionHelper : ICrawler
		{
			private readonly TransformBlock<PropertyBag, PropertyBag> _transformBlock;
			private readonly string _userAgent;

			public CrawlIngestionHelper(TransformBlock<PropertyBag, PropertyBag> transformBlock,
				string userAgent)
			{
				_transformBlock = transformBlock;
				_userAgent = userAgent;
			}

			public void Crawl(Uri uri, PropertyBag referer)
			{
				int depth = referer?.Step?.Depth + 1 ?? 0;
				_transformBlock.Post(new PropertyBag
				{
					Step = new CrawlStep(uri, depth),
					Referrer = referer?.Referrer,
					UserAgent = _userAgent
				});
			}
		}
	}
}