using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using NCrawler.Interfaces;

using Serilog;

namespace NCrawler.Robots
{
	public class RobotsPipelineStep : IPipelineStep
	{
		private readonly HttpClient _httpClient = new HttpClient();
		private readonly ILogger _logger;

		private readonly IDictionary<string, RobotsTxt.Robots> _robotsInfo =
			new Dictionary<string, RobotsTxt.Robots>();

		public RobotsPipelineStep(ILogger logger)
		{
			_logger = logger;
		}

		public async Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			string robotsHttpUrl =
				$"{propertyBag.Step.Uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped).ToLowerInvariant()}/robots.txt";
			RobotsTxt.Robots robots;
			if (!_robotsInfo.TryGetValue(robotsHttpUrl, out robots))
			{
				_logger.Verbose("Downloading robots.txt file from {@0}", robotsHttpUrl);
				string robotsContext = null;
				try
				{
					robotsContext = await _httpClient.GetStringAsync(robotsHttpUrl);
				}
				catch (WebException)
				{
				}
				catch (ProtocolViolationException)
				{
				}
				catch (HttpRequestException)
				{
				}

				robots = new RobotsTxt.Robots(robotsContext ?? string.Empty);
				_robotsInfo.Add(robotsHttpUrl, robots);
			}

			if (!robots.HasRules)
			{
				return true;
			}

			long crawlDelay = robots.CrawlDelay(propertyBag.UserAgent);
			if (crawlDelay > 0)
			{
				await Task.Delay((int) crawlDelay);
			}

			bool result = robots.IsPathAllowed(propertyBag.UserAgent, propertyBag.Step.Uri.ToString());
			propertyBag["RobotsIsPathAllowed"].Name = nameof(RobotsPipelineStep);
			propertyBag["RobotsIsPathAllowed"].Value = result;
			return result;
		}

		public int MaxDegreeOfParallelism { get; } = 1;
	}
}