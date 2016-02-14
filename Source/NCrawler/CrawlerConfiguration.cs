using System;
using System.Collections.Generic;
using System.Linq;
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
		internal readonly List<IPipelineStep> Pipeline = new List<IPipelineStep>();
		internal readonly List<Uri> StartUris = new List<Uri>();
		private readonly ILogger _logger;

		public CrawlerConfiguration()
		{
			_logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.ColoredConsole().CreateLogger();
			AddLoggerStep();
			RemoveDuplicates();
			DownloadStep();
		}

		private void AddLoggerStep()
		{
			LambdaFilter(propertyBag =>
			{
				_logger.Verbose("Uri {uri}", propertyBag.Step.Uri);
				return true;
			});
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
		///     How the crawler should present itself to websites
		/// </summary>
		public string UserAgent { get; set; }

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

		public void DownloadStep()
		{
			AddPipelineStep<DownloadPipelineStep>();
		}

		public void RemoveDuplicates(UriComponents uriSensitivity = UriComponents.AbsoluteUri)
		{
			HashSet<string> linksAlreadyCrawled = new HashSet<string>();
			LambdaFilter(
				propertyBag => !linksAlreadyCrawled.Add(propertyBag.Step.Uri.GetComponents(uriSensitivity, UriFormat.Unescaped)));
		}

		public void MaxCrawlDepth(int maxCrawlDepth)
		{
			LambdaFilter(propertyBag => propertyBag.Step.Depth < maxCrawlDepth);
		}

		public void MaxCrawlCount(int maxCrawlCount)
		{
			int downloadCounter = 0;
			LambdaFilter(propertyBag =>
			{
				downloadCounter++;
				return downloadCounter < maxCrawlCount;
			});
		}

		public void MaximumUrlLength(int maxUrlLength)
		{
			LambdaFilter(propertyBag => propertyBag.Step.Uri.ToString().Length < maxUrlLength);
		}

		public void LambdaFilter(Predicate<PropertyBag> predicate, bool runInParallel = false)
		{
			AddPipelineStep(new LambdaFilterPipelineStep(predicate, runInParallel));
		}

		public void RegexFilter(Regex regex)
		{
			LambdaFilter(bag => regex.Match(bag.Step.Uri.ToString()).Success);
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

			var pipeline = Pipeline
				.Select(pipelineStep =>
				{
					return new TransformBlock<PropertyBag, PropertyBag>(propertyBag =>
					{
						try
						{
							propertyBag.StopPipelining = pipelineStep.Process(propertyBag);
						}
						catch (Exception exception)
						{
							_logger.Error(exception, "Error while processing pipeline step {pipelineStep}", pipelineStep.GetType().Name);
						}

						return propertyBag;
					}, new ExecutionDataflowBlockOptions
					{
						MaxDegreeOfParallelism = pipelineStep.ProcessInParallel ? MaxDegreeOfParallelism : 1
					});
				})
				.ToArray();

			ActionBlock<PropertyBag> terminationCheckerBlock = new ActionBlock<PropertyBag>(propertyBag =>
			{
				if (ingestBlock.InputCount == 0
					&& !ingestBlock.Completion.IsCompleted
					&& !ingestBlock.Completion.IsCanceled
					&& !ingestBlock.Completion.IsFaulted)
				{
					ingestBlock.Complete();
				}
			}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1});

			ingestBlock.LinkTo(ingestBlockForAggregation, new DataflowLinkOptions {PropagateCompletion = true});
			TransformBlock<PropertyBag, PropertyBag> last = ingestBlockForAggregation;
			foreach (TransformBlock<PropertyBag, PropertyBag> transformBlock in pipeline)
			{
				last.LinkTo(transformBlock, new DataflowLinkOptions {PropagateCompletion = true});
				last = transformBlock;
			}

			last.LinkTo(terminationCheckerBlock, new DataflowLinkOptions { PropagateCompletion = true });

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
	}
}