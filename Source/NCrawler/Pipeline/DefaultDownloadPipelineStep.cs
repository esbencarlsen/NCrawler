using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NCrawler.Pipeline
{
	public class DefaultDownloadPipelineStep : IPipelineStep
	{
		public DefaultDownloadPipelineStep(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public async Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			Stopwatch sw = Stopwatch.StartNew();
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(propertyBag.Step.Uri);
			request.Method = "GET";
			try
			{
				using (HttpWebResponse httpWebResponse = (HttpWebResponse) await request.GetResponseAsync())
				using (Stream downloadStream = httpWebResponse.GetResponseStream())
				using (MemoryStream ms = new MemoryStream())
				{
					if (downloadStream != null)
					{
						await downloadStream.CopyToAsync(ms);
					}

					sw.Stop();
					HttpWebResponseToPropertyBag(httpWebResponse, propertyBag);
					propertyBag.Response = ms.ToArray();
					propertyBag.DownloadTime = sw.Elapsed;
				}
			}
			catch (WebException ex)
			{
				HttpWebResponse httpWebResponse = ex.Response as HttpWebResponse;
				HttpWebResponseToPropertyBag(httpWebResponse, propertyBag);
				propertyBag.DownloadTime = TimeSpan.MaxValue;
			}
			catch (ProtocolViolationException)
			{
				propertyBag.StatusCode = HttpStatusCode.Forbidden;
				propertyBag.DownloadTime = TimeSpan.MaxValue;
			}

			return true;
		}

		public int MaxDegreeOfParallelism { get; }

		private static void HttpWebResponseToPropertyBag(HttpWebResponse httpWebResponse, PropertyBag propertyBag)
		{
			if (httpWebResponse == null)
			{
				return;
			}

			propertyBag.CharacterSet = httpWebResponse.CharacterSet;
			propertyBag.ContentEncoding = httpWebResponse.ContentEncoding;
			propertyBag.ContentType = httpWebResponse.ContentType;
			propertyBag.Headers = httpWebResponse.Headers.AllKeys.ToDictionary(key => key, key => (IEnumerable<string>) new [] { httpWebResponse.Headers.Get(key) });
			propertyBag.LastModified = httpWebResponse.LastModified;
			propertyBag.Method = httpWebResponse.Method;
			propertyBag.ProtocolVersion = httpWebResponse.ProtocolVersion;
			propertyBag.ResponseUri = httpWebResponse.ResponseUri;
			propertyBag.Server = httpWebResponse.Server;
			propertyBag.StatusCode = httpWebResponse.StatusCode;
			propertyBag.StatusDescription = httpWebResponse.StatusDescription;
		}
	}
}