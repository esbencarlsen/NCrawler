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
	public class IFilterProcessor : IPipelineStepWithTimeout
	{
		#region Readonly & Static Fields

		protected readonly Dictionary<string, string> m_MimeTypeExtensionMapping =
			new Dictionary<string, string>();

		#endregion

		#region Constructors

		public IFilterProcessor()
		{
			ProcessorTimeout = TimeSpan.FromSeconds(10);
		}

		public IFilterProcessor(string mimeType, string extension)
			: this()
		{
			m_MimeTypeExtensionMapping.Add(mimeType.ToUpperInvariant(), extension);
		}

		#endregion

		#region Instance Methods

		protected virtual string MapContentTypeToExtension(string contentType)
		{
			contentType = contentType.ToLowerInvariant();
			return m_MimeTypeExtensionMapping[contentType];
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