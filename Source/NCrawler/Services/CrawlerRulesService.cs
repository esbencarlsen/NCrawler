using System;
using System.Linq;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Utils;

namespace NCrawler.Services
{
	/// <summary>
	/// Handles logic of how to follow links when crawling
	/// </summary>
	public class CrawlerRulesService : ICrawlerRules
	{
		#region Readonly & Static Fields

		protected readonly Uri BaseUri;
		protected readonly Crawler Crawler;
		protected readonly IRobot Robot;

		#endregion

		#region Constructors

		public CrawlerRulesService(Crawler crawler, IRobot robot, Uri baseUri)
		{
			AspectF.Define.
				NotNull(crawler, "crawler").
				NotNull(robot, "robot").
				NotNull(baseUri, "baseUri");

			Crawler = crawler;
			Robot = robot;
			BaseUri = baseUri;
		}

		#endregion

		#region ICrawlerRules Members

		/// <summary>
		/// 	Checks if the crawler should follow an url
		/// </summary>
		/// <param name = "uri">Url to check</param>
		/// <param name = "referrer"></param>
		/// <returns>True if the crawler should follow the url, else false</returns>
		public virtual bool IsAllowedUrl(Uri uri, CrawlStep referrer)
		{
			if (Crawler.MaximumUrlSize.HasValue && Crawler.MaximumUrlSize.Value > 10 &&
				uri.ToString().Length > Crawler.MaximumUrlSize.Value)
			{
				return false;
			}

			if (!Crawler.IncludeFilter.IsNull() && Crawler.IncludeFilter.Any(f => f.Match(uri, referrer)))
			{
				return true;
			}

			if (!Crawler.ExcludeFilter.IsNull() && Crawler.ExcludeFilter.Any(f => f.Match(uri, referrer)))
			{
				return false;
			}

			if (IsExternalUrl(uri))
			{
				return false;
			}

			return !Crawler.AdhereToRobotRules || Robot.IsAllowed(Crawler.UserAgent, uri);
		}

		public virtual bool IsExternalUrl(Uri uri)
		{
			return BaseUri.IsHostMatch(uri);
		}

		#endregion
	}
}