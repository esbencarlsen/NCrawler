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

		protected readonly Uri m_BaseUri;
		private readonly ILifetimeScope m_LifetimeScope;
		private readonly ThreadSafeCounter m_ThreadInUse = new ThreadSafeCounter();
		private long m_VisitedCount;

		#endregion

		#region Fields

		protected ICrawlerHistory m_CrawlerHistory;
		protected ICrawlerQueue m_CrawlerQueue;
		protected ILog m_Logger;
		protected ITaskRunner m_TaskRunner;
		protected Func<IWebDownloader> m_WebDownloaderFactory;

		private bool m_Cancelled;
		private ManualResetEvent m_CrawlCompleteEvent;
		private bool m_CrawlStopped;
		private ICrawlerRules m_CrawlerRules;
		private bool m_Crawling;
		private long m_DownloadErrors;
		private Stopwatch m_Runtime;
		private bool m_OnlyOneCrawlPerInstance;

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

			m_LifetimeScope = NCrawlerModule.Container.BeginLifetimeScope();
			m_BaseUri = crawlStart;
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
			if (m_OnlyOneCrawlPerInstance)
			{
				throw new InvalidOperationException("Crawler instance cannot be reused");
			}

			m_OnlyOneCrawlPerInstance = true;

			Parameter[] parameters = new Parameter[]
				{
					new TypedParameter(typeof (Uri), m_BaseUri),
					new NamedParameter("crawlStart", m_BaseUri),
					new TypedParameter(typeof (Crawler), this),
				};
			m_CrawlerQueue = m_LifetimeScope.Resolve<ICrawlerQueue>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ICrawlerQueue), m_CrawlerQueue)).ToArray();
			m_CrawlerHistory = m_LifetimeScope.Resolve<ICrawlerHistory>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ICrawlerHistory), m_CrawlerHistory)).ToArray();
			m_TaskRunner = m_LifetimeScope.Resolve<ITaskRunner>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ITaskRunner), m_TaskRunner)).ToArray();
			m_Logger = m_LifetimeScope.Resolve<ILog>(parameters);
			parameters = parameters.AddToEnd(new TypedParameter(typeof (ILog), m_Logger)).ToArray();
			m_CrawlerRules = m_LifetimeScope.Resolve<ICrawlerRules>(parameters);
			m_Logger.Verbose("Crawl started @ {0}", m_BaseUri);
			m_WebDownloaderFactory = m_LifetimeScope.Resolve<Func<IWebDownloader>>();
			using (m_CrawlCompleteEvent = new ManualResetEvent(false))
			{
				m_Crawling = true;
				m_Runtime = Stopwatch.StartNew();

				if (m_CrawlerQueue.Count > 0)
				{
					// Resume enabled
					ProcessQueue();
				}
				else
				{
					AddStep(m_BaseUri, 0);
				}

				if (!m_CrawlStopped)
				{
					m_CrawlCompleteEvent.WaitOne();
				}

				m_Runtime.Stop();
				m_Crawling = false;
			}

			if (m_Cancelled)
			{
				OnCancelled();
			}

			m_Logger.Verbose("Crawl ended @ {0} in {1}", m_BaseUri, m_Runtime.Elapsed);
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
			if (!m_Crawling)
			{
				throw new InvalidOperationException("Crawler must be running before adding steps");
			}

			if (m_CrawlStopped)
			{
				return;
			}

			if ((uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) || // Only accept http(s) schema
				(MaximumCrawlDepth.HasValue && MaximumCrawlDepth.Value > 0 && depth >= MaximumCrawlDepth.Value) ||
				!m_CrawlerRules.IsAllowedUrl(uri, referrer) ||
				!m_CrawlerHistory.Register(uri.GetUrlKeyString(UriSensitivity)))
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
					IsExternalUrl = m_CrawlerRules.IsExternalUrl(uri),
					IsAllowed = true,
				};
			m_CrawlerQueue.Push(new CrawlerQueueEntry
				{
					CrawlStep = crawlStep,
					Referrer = referrer,
					Properties = properties
				});
			m_Logger.Verbose("Added {0} to queue referred from {1}",
				crawlStep.Uri, referrer.IsNull() ? string.Empty : referrer.Uri.ToString());
			ProcessQueue();
		}

		public void Cancel()
		{
			if (!m_Crawling)
			{
				throw new InvalidOperationException("Crawler must be running before cancellation is possible");
			}

			m_Logger.Verbose("Cancelled crawler from {0}", m_BaseUri);
			if (m_Cancelled)
			{
				throw new ConstraintException("Already cancelled once");
			}

			m_Cancelled = true;
			StopCrawl();
		}

		protected override void Cleanup()
		{
			m_LifetimeScope.Dispose();
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
				m_Logger.Debug("Executing pipeline step {0}", pipelineStep.GetType().Name);
				if (pipelineStep is IPipelineStepWithTimeout)
				{
					IPipelineStepWithTimeout stepWithTimeout = (IPipelineStepWithTimeout) pipelineStep;
					m_Logger.Debug("Running pipeline step {0} with timeout {1}",
						pipelineStep.GetType().Name, stepWithTimeout.ProcessorTimeout);
					m_TaskRunner.RunSync(cancelArgs =>
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

				m_Logger.Debug("Executed pipeline step {0} in {1}", pipelineStep.GetType().Name, sw.Elapsed);
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
				m_CrawlCompleteEvent.Set();
				return;
			}

			if (m_CrawlStopped)
			{
				if (ThreadsInUse == 0)
				{
					m_CrawlCompleteEvent.Set();
				}

				return;
			}

			if (MaximumCrawlTime.HasValue && m_Runtime.Elapsed > MaximumCrawlTime.Value)
			{
				m_Logger.Verbose("Maximum crawl time({0}) exceeded, cancelling", MaximumCrawlTime.Value);
				StopCrawl();
				return;
			}

			if (MaximumCrawlCount.HasValue && MaximumCrawlCount.Value > 0 &&
				MaximumCrawlCount.Value <= Interlocked.Read(ref m_VisitedCount))
			{
				m_Logger.Verbose("CrawlCount exceeded {0}, cancelling", MaximumCrawlCount.Value);
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
			CrawlerQueueEntry crawlerQueueEntry = m_CrawlerQueue.Pop();
			if (crawlerQueueEntry.IsNull() || !OnBeforeDownload(crawlerQueueEntry.CrawlStep))
			{
				return;
			}

			IWebDownloader webDownloader = m_WebDownloaderFactory();
			webDownloader.MaximumDownloadSizeInRam = MaximumDownloadSizeInRam;
			webDownloader.ConnectionTimeout = ConnectionTimeout;
			webDownloader.MaximumContentSize = MaximumContentSize;
			webDownloader.DownloadBufferSize = DownloadBufferSize;
			webDownloader.UserAgent = UserAgent;
			webDownloader.UseCookies = UseCookies;
			webDownloader.ReadTimeout = ConnectionReadTimeout;
			webDownloader.RetryCount = DownloadRetryCount;
			webDownloader.RetryWaitDuration = DownloadRetryWaitDuration;
			m_Logger.Verbose("Downloading {0}", crawlerQueueEntry.CrawlStep.Uri);
			ThreadSafeCounter.ThreadSafeCounterCookie threadSafeCounterCookie = m_ThreadInUse.EnterCounterScope(crawlerQueueEntry);
			Interlocked.Increment(ref m_VisitedCount);
			webDownloader.DownloadAsync(crawlerQueueEntry.CrawlStep, crawlerQueueEntry.Referrer, DownloadMethod.GET,
				EndDownload, OnDownloadProgress, threadSafeCounterCookie);
		}

		private void StopCrawl()
		{
			if (m_CrawlStopped)
			{
				return;
			}

			m_CrawlStopped = true;
			if (ThreadsInUse == 0)
			{
				m_CrawlCompleteEvent.Set();
				return;
			}
		}

		#endregion
	}
}