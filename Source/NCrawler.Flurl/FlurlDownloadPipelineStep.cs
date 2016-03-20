using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Flurl.Http;

namespace NCrawler.Flurl
{
	public class FlurlDownloadPipelineStep : IPipelineStep
	{
		public const string FlurlHttpCallPropertyName = "FlurlHttpCall";

		public FlurlDownloadPipelineStep(int maxDegreeOfParallelism)
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public async Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			FlurlClient client = propertyBag.Step.Uri.ToString()
				.ConfigureHttpClient(httpClient => { });
			client.Settings.AfterCall += httpCall =>
			{
				propertyBag[FlurlHttpCallPropertyName].Value = httpCall;
				propertyBag.DownloadTime = httpCall.Duration.GetValueOrDefault();
			};

			HttpResponseMessage getResult = await client.GetAsync();
			propertyBag.CharacterSet = getResult.Content.Headers.ContentType.CharSet;
			propertyBag.ContentEncoding = string.Join(";", getResult.Content.Headers.ContentEncoding);
			propertyBag.ContentType = getResult.Content.Headers.ContentType.MediaType;
			propertyBag.Headers = getResult.Content.Headers.ToDictionary(x => x.Key, x => x.Value);
			propertyBag.LastModified = getResult.Headers.Date.GetValueOrDefault(DateTimeOffset.UtcNow).DateTime;
			propertyBag.Method = "GET";
			//propertyBag.ProtocolVersion = getResult.;
			//propertyBag.ResponseUri = getResult.Headers.Server;
			propertyBag.Server = string.Join(";", getResult.Headers.Server.Select(x => x.Product.ToString()));
			propertyBag.StatusCode = getResult.StatusCode;
			propertyBag.StatusDescription = getResult.StatusCode.ToString();
			propertyBag.Response = await getResult.Content.ReadAsByteArrayAsync();
			return true;
		}

		public int MaxDegreeOfParallelism { get; }
	}
}