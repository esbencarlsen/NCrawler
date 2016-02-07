using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using Autofac;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Services;
using NCrawler.Utils;

namespace NCrawler.LanguageDetection.Google
{
	public class GoogleLanguageDetection : IPipelineStepWithTimeout
	{
		#region Constants

		private const int MaxPostSize = 900;

		#endregion

		#region Readonly & Static Fields

		private readonly ILog m_Logger;

		#endregion

		#region Constructors

		public GoogleLanguageDetection()
		{
			m_Logger = NCrawlerModule.Container.Resolve<ILog>();
		}

		#endregion

		#region IPipelineStepWithTimeout Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			AspectF.Define.
				NotNull(crawler, "crawler").
				NotNull(propertyBag, "propertyBag");

			string content = propertyBag.Text;
			if (content.IsNullOrEmpty())
			{
				return;
			}

			string contentLookupText = content.Max(MaxPostSize);
			string encodedRequestUrlFragment =
				"http://ajax.googleapis.com/ajax/services/language/detect?v=1.0&q={0}".FormatWith(contentLookupText);

			m_Logger.Verbose("Google language detection using: {0}", encodedRequestUrlFragment);

			try
			{
				IWebDownloader downloader = NCrawlerModule.Container.Resolve<IWebDownloader>();
				PropertyBag result = downloader.Download(new CrawlStep(new Uri(encodedRequestUrlFragment), 0), null, DownloadMethod.GET);
				if (result.IsNull())
				{
					return;
				}

				using (Stream responseReader = result.GetResponse())
				using (StreamReader reader = new StreamReader(responseReader))
				{
					string json = reader.ReadLine();
					using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
					{
						DataContractJsonSerializer ser =
							new DataContractJsonSerializer(typeof (LanguageDetector));
						LanguageDetector detector = ser.ReadObject(ms) as LanguageDetector;

						if (!detector.IsNull())
						{
							CultureInfo culture = CultureInfo.GetCultureInfo(detector.responseData.language);
							propertyBag["Language"].Value = detector.responseData.language;
							propertyBag["LanguageCulture"].Value = culture;
						}
					}
				}
			}
			catch (Exception ex)
			{
				m_Logger.Error("Error during google language detection, the error was: {0}", ex.ToString());
			}
		}

		public TimeSpan ProcessorTimeout
		{
			get { return TimeSpan.FromSeconds(10); }
		}

		#endregion
	}

	[Serializable]
	public class LanguageDetector
	{
		#region Fields

		public LanguageDetectionResponseData responseData = new LanguageDetectionResponseData();
		public string responseDetails;
		public string responseStatus;

		#endregion
	}

	[Serializable]
	public class LanguageDetectionResponseData
	{
		#region Fields

		public string confidence;
		public string isReliable;
		public string language;

		#endregion
	}
}