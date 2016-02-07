using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

using EPocalipse.IFilter;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Utils;

namespace NCrawler.IFilterProcessor
{
	public class FilterProcessor : IPipelineStepWithTimeout
	{
		#region Readonly & Static Fields

		protected readonly Dictionary<string, string> MimeTypeExtensionMapping =
			new Dictionary<string, string>();

		#endregion

		#region Constructors

		public FilterProcessor()
		{
			ProcessorTimeout = TimeSpan.FromSeconds(10);
		}

		public FilterProcessor(string mimeType, string extension)
			: this()
		{
			MimeTypeExtensionMapping.Add(mimeType.ToUpperInvariant(), extension);
		}

		#endregion

		#region Instance Methods

		protected virtual string MapContentTypeToExtension(string contentType)
		{
			contentType = contentType.ToLowerInvariant();
			return MimeTypeExtensionMapping[contentType];
		}

		#endregion

		#region IPipelineStepWithTimeout Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK)
			{
				return;
			}

			string extension = MapContentTypeToExtension(propertyBag.ContentType);
			if (extension.IsNullOrEmpty())
			{
				return;
			}

			propertyBag.Title = propertyBag.Step.Uri.PathAndQuery;
			using (TempFile temp = new TempFile())
			{
				temp.FileName += "." + extension;
				using (FileStream fs = new FileStream(temp.FileName, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000))
				using (Stream input = propertyBag.GetResponse())
				{
					input.CopyToStream(fs);
				}

				using (FilterReader filterReader = new FilterReader(temp.FileName))
				{
					string content = filterReader.ReadToEnd();
					propertyBag.Text = content.Trim();
				}
			}
		}

		public TimeSpan ProcessorTimeout { get; set; }

		#endregion
	}
}