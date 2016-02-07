using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.Services
{
	/// <summary>
	/// 	Taken from Searcharoo 7, and modifed
	/// </summary>
	public class RobotService : IRobot
	{
		#region Readonly & Static Fields

		private readonly Uri m_StartPageUri;
		private readonly IWebDownloader m_WebDownloader;

		#endregion

		#region Fields

		private string[] m_DenyUrls = new string[0];

		private bool m_Initialized;

		#endregion

		#region Constructors

		public RobotService(Uri startPageUri, IWebDownloader webDownloader)
		{
			m_StartPageUri = startPageUri;
			m_WebDownloader = webDownloader;
		}

		#endregion

		#region Instance Methods

		/// <summary>
		/// 	Does the parsed robots.txt file allow this Uri to be spidered for this user-agent?
		/// </summary>
		/// <remarks>
		/// 	This method does all its "matching" in uppercase - it expects the _DenyUrl 
		/// 	elements to be ToUpper() and it calls ToUpper on the passed-in Uri...
		/// </remarks>
		public bool Allowed(Uri uri)
		{
			if (!m_Initialized)
			{
				Initialize();
				m_Initialized = true;
			}

			if (m_DenyUrls.Length == 0)
			{
				return true;
			}

			string url = uri.AbsolutePath.ToUpperInvariant();
			if (m_DenyUrls.
				Where(denyUrlFragment => url.Length >= denyUrlFragment.Length).
				Any(denyUrlFragment => url.Substring(0, denyUrlFragment.Length) == denyUrlFragment))
			{
				return false;
			}

			return !url.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase);
		}

		private void Initialize()
		{
			try
			{
				Uri robotsUri = new Uri("http://{0}/robots.txt".FormatWith(m_StartPageUri.Host));
				PropertyBag robots = m_WebDownloader.Download(new CrawlStep(robotsUri, 0), null, DownloadMethod.GET);

				if (robots == null || robots.StatusCode != HttpStatusCode.OK)
				{
					return;
				}

				string fileContents;
				using (StreamReader stream = new StreamReader(robots.GetResponse(), Encoding.ASCII))
				{
					fileContents = stream.ReadToEnd();
				}

				string[] fileLines = fileContents.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

				bool rulesApply = false;
				List<string> rules = new List<string>();
				foreach (string line in fileLines)
				{
					RobotInstruction ri = new RobotInstruction(line);
					if (!ri.Instruction.IsNullOrEmpty())
					{
						switch (ri.Instruction[0])
						{
							case '#': //then comment - ignore
								break;
							case 'u': // User-Agent
								if ((ri.UrlOrAgent.IndexOf("*") >= 0) || (ri.UrlOrAgent.IndexOf(m_WebDownloader.UserAgent) >= 0))
								{
									// these rules apply
									rulesApply = true;
								}
								else
								{
									rulesApply = false;
								}
								break;
							case 'd': // Disallow
								if (rulesApply)
								{
									rules.Add(ri.UrlOrAgent.ToUpperInvariant());
								}
								break;
							case 'a': // Allow
								break;
							default:
								// empty/unknown/error
								break;
						}
					}
				}

				m_DenyUrls = rules.ToArray();
			}
			catch (Exception)
			{
			}
		}

		#endregion

		#region IRobot Members

		public bool IsAllowed(string userAgent, Uri uri)
		{
			return Allowed(uri);
		}

		#endregion

		#region Nested type: RobotInstruction

		/// <summary>
		/// 	Use this class to read/parse the robots.txt file
		/// </summary>
		/// <remarks>
		/// 	Types of data coming into this class
		/// 	User-agent: * ==> _Instruction='User-agent', _Url='*'
		/// 	Disallow: /cgi-bin/ ==> _Instruction='Disallow', _Url='/cgi-bin/'
		/// 	Disallow: /tmp/ ==> _Instruction='Disallow', _Url='/tmp/'
		/// 	Disallow: /~joe/ ==> _Instruction='Disallow', _Url='/~joe/'
		/// </remarks>
		private class RobotInstruction
		{
			#region Constructors

			/// <summary>
			/// 	Constructor requires a line, hopefully in the format [instuction]:[url]
			/// </summary>
			public RobotInstruction(string line)
			{
				UrlOrAgent = string.Empty;
				string instructionLine = line;
				int commentPosition = instructionLine.IndexOf('#');
				if (commentPosition == 0)
				{
					Instruction = "#";
				}

				if (commentPosition >= 0)
				{
					// comment somewhere on the line, trim it off
					instructionLine = instructionLine.Substring(0, commentPosition);
				}

				if (instructionLine.Length > 0)
				{
					// wasn't just a comment line (which should have been filtered out before this anyway
					string[] lineArray = instructionLine.Split(':');
					Instruction = lineArray[0].Trim().ToUpperInvariant();
					if (lineArray.Length > 1)
					{
						UrlOrAgent = lineArray[1].Trim();
					}
				}
			}

			#endregion

			#region Instance Properties

			/// <summary>
			/// 	Upper-case part of robots.txt line, before the colon (:)
			/// </summary>
			public string Instruction { get; private set; }

			/// <summary>
			/// 	Upper-case part of robots.txt line, after the colon (:)
			/// </summary>
			public string UrlOrAgent { get; private set; }

			#endregion
		}

		#endregion
	}
}