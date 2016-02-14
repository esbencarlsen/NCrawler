using System.Diagnostics;
using System.IO;
using System.Net;

using NCrawler.Interfaces;

namespace NCrawler.Pipeline
{
	public class DownloadPipelineStep : IPipelineStep
	{
		public bool Process(PropertyBag propertyBag)
		{
			Stopwatch sw = Stopwatch.StartNew();
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(propertyBag.Step.Uri);
			request.Method = "GET";
			using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
			{
				Stream downloadStream = response.GetResponseStream();
				sw.Stop();
				propertyBag.CharacterSet = response.CharacterSet;
				propertyBag.ContentEncoding = response.ContentEncoding;
				propertyBag.ContentType = response.ContentType;
				propertyBag.Headers = response.Headers;
				propertyBag.IsMutuallyAuthenticated = response.IsMutuallyAuthenticated;
				propertyBag.LastModified = response.LastModified;
				propertyBag.Method = response.Method;
				propertyBag.ProtocolVersion = response.ProtocolVersion;
				propertyBag.ResponseUri = response.ResponseUri;
				propertyBag.Server = response.Server;
				propertyBag.StatusCode = response.StatusCode;
				propertyBag.StatusDescription = response.StatusDescription;
				propertyBag.GetResponse = () => downloadStream;
				propertyBag.DownloadTime = sw.Elapsed;
			}

			return true;
		}

		public bool ProcessInParallel { get; } = true;
	}
}