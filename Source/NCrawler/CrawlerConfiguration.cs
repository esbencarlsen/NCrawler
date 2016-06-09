using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NCrawler.Extensions;
using NCrawler.Pipeline;
using NCrawler.Utils;

using Serilog;

namespace NCrawler
{
	public partial class CrawlerConfiguration
	{
		internal readonly List<IPipelineStep> Pipeline = new List<IPipelineStep>();
		internal readonly List<Uri> StartUris = new List<Uri>();

		private string _userAgent = "Mozilla";

		public CrawlerConfiguration()
		{
			RemoveDotNetServicePointRestrictions();
			SetDefaultServicePointManagerSettings();

			Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.ColoredConsole().CreateLogger();
		}

		public ILogger Logger { get; set; }

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
			Download();
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

		public CrawlerConfiguration AddLoggerStep()
		{
			return Where((crawler, propertyBag) =>
			{
				Logger.Verbose("Uri {uri}", propertyBag.Step.Uri);
				return true;
			});
		}

		/// <summary>
		/// This step does the actual download
		/// </summary>
		/// <param name="maxDegreeOfParallelism"></param>
		/// <returns></returns>
		public CrawlerConfiguration Download(int? maxDegreeOfParallelism = null)
		{
			return AddPipelineStep(new DefaultDownloadPipelineStep(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
		}

		public CrawlerConfiguration RemoveDuplicates(UriComponents uriSensitivity = UriComponents.AbsoluteUri)
		{
			HashSet<string> linksAlreadyCrawled = new HashSet<string>();
			return Where((crawler, propertyBag) =>
				linksAlreadyCrawled.Add(propertyBag.Step.Uri.GetUrlKeyString(uriSensitivity)));
		}

		public CrawlerConfiguration DownloadDelay(TimeSpan delayFor)
		{
			return Do(async (crawler, propertyBag) => await Task.Delay(delayFor));
		}

		public CrawlerConfiguration MaxCrawlDepth(int maxCrawlDepth)
		{
			return Where((crawler, propertyBag) => propertyBag.Step.Depth < maxCrawlDepth);
		}

		public CrawlerConfiguration MaxCrawlCount(int maxCrawlCount)
		{
			int downloadCounter = 0;
			return Where((crawler, propertyBag) => downloadCounter++ < maxCrawlCount);
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
			return Where((crawler, propertyBag) => propertyBag.Step.Uri.ToString().Length < maxUrlLength);
		}

		public CrawlerConfiguration Do(Action<ICrawler, PropertyBag> predicate, int maxDegreeOfParallelism = 1)
		{
			return AddPipelineStep(new LambdaFilterPipelineStep((crawler, bag) =>
			{
				predicate(crawler, bag);
				return true;
			}, maxDegreeOfParallelism));
		}

		public CrawlerConfiguration Where(Func<ICrawler, PropertyBag, bool> predicate, int maxDegreeOfParallelism = 1)
		{
			return AddPipelineStep(new LambdaFilterPipelineStep(predicate, maxDegreeOfParallelism));
		}

		public CrawlerConfiguration Where(Func<ICrawler, PropertyBag, Task<bool>> predicate,
			int maxDegreeOfParallelism = 1)
		{
			return AddPipelineStep(new LambdaFilterPipelineStep(predicate, maxDegreeOfParallelism));
		}

		public CrawlerConfiguration UrlRegexFilter(Regex regex)
		{
			return Where((crawler, propertyBag) => regex.Match(propertyBag.Step.Uri.ToString()).Success);
		}

		public CrawlerConfiguration ExtractEmail(int? maxDegreeOfParallelism = null)
		{
			return AddPipelineStep(
				new EMailEntityExtractionProcessor(maxDegreeOfParallelism.GetValueOrDefault(Environment.ProcessorCount)));
		}

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

		public CrawlerConfiguration WhereHostInCrawlSeed()
		{
			return Where((crawler, bag) => { return StartUris.Any(host => host.IsHostMatch(bag.Step.Uri)); });
		}

		public CrawlerConfiguration CrawlSeed(string url)
		{
			AspectF.Define.NotNull(url, nameof(url));
			StartUris.Add(new Uri(url));
			return this;
		}

		public CrawlerConfiguration CrawlSeed(Uri uri)
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
	}
}