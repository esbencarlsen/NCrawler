using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Autofac;
using Autofac.Core;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Services;
using NCrawler.Utils;

namespace NCrawler
{
	public partial class Crawler : DisposableBase
	{
		#region Readonly & Static Fields

		protected readonly Uri BaseUri;
		private readonly ILifetimeScope _lifetimeScope;
		private readonly ThreadSafeCounter _threadInUse = new ThreadSafeCounter();
		private long _visitedCount;

		#endregion

		#region Fields

		protected ICrawlerHistory CrawlerHistory;
		protected ICrawlerQueue CrawlerQueue;
		protected ILog Logger;
		protected ITaskRunner TaskRunner;
		protected Func<IWebDownloader> WebDownloaderFactory;

		private bool _cancelled;
		private ManualResetEvent _crawlCompleteEvent;
		private bool _crawlStopped;
		private ICrawlerRules _crawlerRules;
		private bool _crawling;
		private long _downloadErrors;
		private Stopwatch _runtime;
		private bool _onlyOneCrawlPerInstance;

		#endregion

		#region Constructors

		/// <summary>
		/// 	Constructor for NCrawler
		/// </summary>
		/// <param name = "crawlStart">The url from where the crawler should start</param>
		/// <param name = "pipeline">Pipeline steps</param>
		public Crawler(Uri crawlStart, params IPipelineStep[] pipeline)
		{
			AspectF.Define.
				NotNull(crawlStart, "crawlStart").
				NotNull(pipeline, "pipeline");

			_lifetimeScope = NCrawlerModule.Container.BeginLifetimeScope();
			BaseUri = crawlStart;
			MaximumCrawlDepth = null;
			AdhereToRobotRules = true;
			MaximumThreadCount = 1;
			Pipeline = pipeline;
			UriSensitivity = UriComponents.HttpRequestUrl;
			MaximumDownloadSizeInRam = 1024*1024;
			DownloadBufferSize = 50 * 1024;
		}

		#endregion

		#region Instance Methods

		/// <summary>
		/// 	Start crawl process
		/// </summary>
		public virtual void Crawl()
		{
			if (_onlyOneCrawlPerInstance)
			{
				throw new InvalidOperationException("Crawler instance cannot be reused");
			}

			_onlyOneCrawlPerInstance = true;

			Parameter[] parameters = new Parameter[]
				{
					new TypedParameter(typeof (Uri), BaseUri),
					new NamedParameter("crawlStart", BaseUri),
					new TypedParameter(typeof (Crawler), this),
				};
			CrawlerQueue = _lifetimeScope.Resolve<ICrawlerQueue>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ICrawlerQueue), CrawlerQueue)).ToArray();
			CrawlerHistory = _lifetimeScope.Resolve<ICrawlerHistory>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ICrawlerHistory), CrawlerHistory)).ToArray();
			TaskRunner = _lifetimeScope.Resolve<ITaskRunner>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ITaskRunner), TaskRunner)).ToArray();
			Logger = _lifetimeScope.Resolve<ILog>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ILog), Logger)).ToArray();
			_crawlerRules = _lifetimeScope.Resolve<ICrawlerRules>(parameters);
			Logger.Verbose("Crawl started @ {0}", BaseUri);
			WebDownloaderFactory = _lifetimeScope.Resolve<Func<IWebDownloader>>();
			using (_crawlCompleteEvent = new ManualResetEvent(false))
			{
				_crawling = true;
				_runtime = Stopwatch.StartNew();

				if (CrawlerQueue.Count > 0)
				{
					// Resume enabled
					ProcessQueue();
				}
				else
				{
					AddStep(BaseUri, 0);
				}

				if (!_crawlStopped)
				{
					_crawlCompleteEvent.WaitOne();
				}

				_runtime.Stop();
				_crawling = false;
			}

			if (_cancelled)
			{
				OnCancelled();
			}

			Logger.Verbose("Crawl ended @ {0} in {1}", BaseUri, _runtime.Elapsed);
			OnCrawlFinished();
		}

		/// <summary>
		/// 	Queue a new step on the crawler queue
		/// </summary>
		/// <param name = "uri">url to crawl</param>
		/// <param name = "depth">depth of the url</param>
		public void AddStep(Uri uri, int depth)
		{
			AddStep(uri, depth, null, null);
		}

		/// <summary>
		/// 	Queue a new step on the crawler queue
		/// </summary>
		/// <param name = "uri">url to crawl</param>
		/// <param name = "depth">depth of the url</param>
		/// <param name = "referrer">Step which the url was located</param>
		/// <param name = "properties">Custom properties</param>
		public void AddStep(Uri uri, int depth, CrawlStep referrer, Dictionary<string, object> properties)
		{
			if (!_crawling)
			{
				throw new InvalidOperationException("Crawler must be running before adding steps");
			}

			if (_crawlStopped)
			{
				return;
			}

			if ((uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) || // Only accept http(s) schema
				(MaximumCrawlDepth.HasValue && MaximumCrawlDepth.Value > 0 && depth >= MaximumCrawlDepth.Value) ||
				!_crawlerRules.IsAllowedUrl(uri, referrer) ||
				!CrawlerHistory.Register(uri.GetUrlKeyString(UriSensitivity)))
			{
				if (depth == 0)
				{
					StopCrawl();
				}

				return;
			}

			// Make new crawl step
			CrawlStep crawlStep = new CrawlStep(uri, depth)
				{
					IsExternalUrl = _crawlerRules.IsExternalUrl(uri),
					IsAllowed = true,
				};
			CrawlerQueue.Push(new CrawlerQueueEntry
				{
					CrawlStep = crawlStep,
					Referrer = referrer,
					Properties = properties
				});
			Logger.Verbose("Added {0} to queue referred from {1}",
				crawlStep.Uri, referrer.IsNull() ? string.Empty : referrer.Uri.ToString());
			ProcessQueue();
		}

		public void Cancel()
		{
			if (!_crawling)
			{
				throw new InvalidOperationException("Crawler must be running before cancellation is possible");
			}

			Logger.Verbose("Cancelled crawler from {0}", BaseUri);
			if (_cancelled)
			{
				throw new ConstraintException("Already cancelled once");
			}

			_cancelled = true;
			StopCrawl();
		}

		protected override void Cleanup()
		{
			_lifetimeScope.Dispose();
		}

		private void EndDownload(RequestState<ThreadSafeCounter.ThreadSafeCounterCookie> requestState)
		{
			using (requestState.State)
			{
				if (requestState.Exception != null)
				{
					OnDownloadException(requestState.Exception, requestState.CrawlStep, requestState.Referrer);
				}

				if (!requestState.PropertyBag.IsNull())
				{
					requestState.PropertyBag.Referrer = requestState.CrawlStep;

					// Assign initial properties to propertybag
					if (!requestState.State.CrawlerQueueEntry.Properties.IsNull())
					{
						requestState.State.CrawlerQueueEntry.Properties.
							ForEach(key => requestState.PropertyBag[key.Key].Value = key.Value);
					}

					if (OnAfterDownload(requestState.CrawlStep, requestState.PropertyBag))
					{
						// Executes all the pipelines sequentially for each downloaded content
						// in the crawl process. Used to extract data from content, like which
						// url's to follow, email addresses, aso.
						Pipeline.ForEach(pipelineStep => ExecutePipeLineStep(pipelineStep, requestState.PropertyBag));
					}
				}
			}

			ProcessQueue();
		}

		private void ExecutePipeLineStep(IPipelineStep pipelineStep, PropertyBag propertyBag)
		{
			try
			{
				Stopwatch sw = Stopwatch.StartNew();
				Logger.Debug("Executing pipeline step {0}", pipelineStep.GetType().Name);
				if (pipelineStep is IPipelineStepWithTimeout)
				{
					IPipelineStepWithTimeout stepWithTimeout = (IPipelineStepWithTimeout) pipelineStep;
					Logger.Debug("Running pipeline step {0} with timeout {1}",
						pipelineStep.GetType().Name, stepWithTimeout.ProcessorTimeout);
					TaskRunner.RunSync(cancelArgs =>
						{
							if (!cancelArgs.Cancel)
							{
								pipelineStep.Process(this, propertyBag);
							}
						}, stepWithTimeout.ProcessorTimeout);
				}
				else
				{
					pipelineStep.Process(this, propertyBag);
				}

				Logger.Debug("Executed pipeline step {0} in {1}", pipelineStep.GetType().Name, sw.Elapsed);
			}
			catch (Exception ex)
			{
				OnProcessorException(propertyBag, ex);
			}
		}

		private void ProcessQueue()
		{
			if (ThreadsInUse == 0 && WaitingQueueLength == 0)
			{
				_crawlCompleteEvent.Set();
				return;
			}

			if (_crawlStopped)
			{
				if (ThreadsInUse == 0)
				{
					_crawlCompleteEvent.Set();
				}

				return;
			}

			if (MaximumCrawlTime.HasValue && _runtime.Elapsed > MaximumCrawlTime.Value)
			{
				Logger.Verbose("Maximum crawl time({0}) exceeded, cancelling", MaximumCrawlTime.Value);
				StopCrawl();
				return;
			}

			if (MaximumCrawlCount.HasValue && MaximumCrawlCount.Value > 0 &&
				MaximumCrawlCount.Value <= Interlocked.Read(ref _visitedCount))
			{
				Logger.Verbose("CrawlCount exceeded {0}, cancelling", MaximumCrawlCount.Value);
				StopCrawl();
				return;
			}

			while (ThreadsInUse < MaximumThreadCount && WaitingQueueLength > 0)
			{
				StartDownload();
			}
		}

		private void StartDownload()
		{
			CrawlerQueueEntry crawlerQueueEntry = CrawlerQueue.Pop();
			if (crawlerQueueEntry.IsNull() || !OnBeforeDownload(crawlerQueueEntry.CrawlStep))
			{
				return;
			}

			IWebDownloader webDownloader = WebDownloaderFactory();
			webDownloader.MaximumDownloadSizeInRam = MaximumDownloadSizeInRam;
			webDownloader.ConnectionTimeout = ConnectionTimeout;
			webDownloader.MaximumContentSize = MaximumContentSize;
			webDownloader.DownloadBufferSize = DownloadBufferSize;
			webDownloader.UserAgent = UserAgent;
			webDownloader.UseCookies = UseCookies;
			webDownloader.ReadTimeout = ConnectionReadTimeout;
			webDownloader.RetryCount = DownloadRetryCount;
			webDownloader.RetryWaitDuration = DownloadRetryWaitDuration;
			Logger.Verbose("Downloading {0}", crawlerQueueEntry.CrawlStep.Uri);
			ThreadSafeCounter.ThreadSafeCounterCookie threadSafeCounterCookie = _threadInUse.EnterCounterScope(crawlerQueueEntry);
			Interlocked.Increment(ref _visitedCount);
			webDownloader.DownloadAsync(crawlerQueueEntry.CrawlStep, crawlerQueueEntry.Referrer, DownloadMethod.Get,
				EndDownload, OnDownloadProgress, threadSafeCounterCookie);
		}

		private void StopCrawl()
		{
			if (_crawlStopped)
			{
				return;
			}

			_crawlStopped = true;
			if (ThreadsInUse == 0)
			{
				_crawlCompleteEvent.Set();
				return;
			}
		}

		#endregion
	}
}