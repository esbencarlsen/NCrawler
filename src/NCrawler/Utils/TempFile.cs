using System.IO;

namespace NCrawler.Utils
{
	/// <summary>
	/// TempFile creates a temp file, that deleted itself when garbage collected
	/// </summary>
	public class TempFile : DisposableBase
	{
		public TempFile()
		{
			FileName = GetTempFileName();
		}

		public TempFile(string fileName)
		{
			FileName = fileName;
		}

		public string FileName { get; set; }
		public string Tag { get; set; }

		/// <summary>
		/// Do cleanup here
		/// </summary>
		protected override void Cleanup()
		{
			FileInfo fi = new FileInfo(FileName);
			if (!fi.Exists)
			{
				return;
			}

			fi.Attributes = FileAttributes.Normal;
			fi.Delete();
		}

		public static string GetTempFileName()
		{
			return Path.GetTempFileName();
		}
	}
}