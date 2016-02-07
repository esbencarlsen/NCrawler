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

		protected readonly Uri m_BaseUri;
		protected readonly Crawler m_Crawler;
		protected readonly IRobot m_Robot;

		#endregion

		#region Constructors

		public CrawlerRulesService(Crawler crawler, IRobot robot, Uri baseUri)
		{
			AspectF.Define.
				NotNull(crawler, "crawler").
				NotNull(robot, "robot").
				NotNull(baseUri, "baseUri");

			m_Crawler = crawler;
			m_Robot = robot;
			m_BaseUri = baseUri;
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
			if (m_Crawler.MaximumUrlSize.HasValue && m_Crawler.MaximumUrlSize.Value > 10 &&
				uri.ToString().Length > m_Crawler.MaximumUrlSize.Value)
			{
				return false;
			}

			if (!m_Crawler.IncludeFilter.IsNull() && m_Crawler.IncludeFilter.Any(f => f.Match(uri, referrer)))
			{
				return true;
			}

			if (!m_Crawler.ExcludeFilter.IsNull() && m_Crawler.ExcludeFilter.Any(f => f.Match(uri, referrer)))
			{
				return false;
			}

			if (IsExternalUrl(uri))
			{
				return false;
			}

			return !m_Crawler.AdhereToRobotRules || m_Robot.IsAllowed(m_Crawler.UserAgent, uri);
		}

		public virtual bool IsExternalUrl(Uri uri)
		{
			return m_BaseUri.IsHostMatch(uri);
		}

		#endregion
	}
}