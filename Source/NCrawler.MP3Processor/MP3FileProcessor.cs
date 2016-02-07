using System.IO;
using System.Net;

using NCrawler.Extensions;
using NCrawler.Interfaces;
using NCrawler.Utils;

using TagLib;

using File = TagLib.File;

namespace NCrawler.MP3Processor
{
	public class Mp3FileProcessor : IPipelineStep
	{
		#region IPipelineStep Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			if (propertyBag.StatusCode != HttpStatusCode.OK)
			{
				return;
			}

			using (TempFile tempFile = new TempFile())
			{
				using (FileStream fs = new FileStream(tempFile.FileName, FileMode.Create, FileAccess.Write, FileShare.Read, 0x1000))
				using (Stream input = propertyBag.GetResponse())
				{
					input.CopyToStream(fs);

					File file = File.Create(new StreamFileAbstraction("", input, input));
					propertyBag["MP3_Album"].Value = file.Tag.Album;
					propertyBag["MP3_Artist"].Value = string.Join(";", file.Tag.AlbumArtists);
					propertyBag["MP3_Comments"].Value = file.Tag.Comment;
					propertyBag["MP3_Genre"].Value = string.Join(";", file.Tag.Genres);
					propertyBag["MP3_Title"].Value = file.Tag.Title;
				}
			}
		}

		#endregion
	}
}