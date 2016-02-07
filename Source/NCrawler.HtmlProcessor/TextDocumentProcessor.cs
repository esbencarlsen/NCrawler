using System;
using System.IO;
using System.Net;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.HtmlProcessor
{
	public class TextDocumentProcessor : IPipelineStep
	{
		#region IPipelineStep Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK)
			{
				return;
			}

			if (!IsTextContent(propertyBag.ContentType))
			{
				return;
			}

			using (Stream reader = propertyBag.GetResponse())
			{
				string content = reader.ReadToEnd();
				propertyBag.Text = content.Trim();
			}
		}

		#endregion

		#region Class Methods

		private static bool IsTextContent(string contentType)
		{
			return contentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}