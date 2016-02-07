using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NCrawler.Utils
{
	internal class FileSystemHelpers
	{
		#region Constants

		private const long ErrorSharingViolation = 32;
		private const long FileAttributeDirectory = 0x10;

		#endregion

		#region Class Methods

		internal static bool FileExists(string fileName)
		{
			long nAttr = GetFileAttributes2(fileName);
			if ((nAttr & FileAttributeDirectory) != FileAttributeDirectory)
			{
				return true;
			}

			int lastError = Marshal.GetLastWin32Error();
			return lastError == ErrorSharingViolation;
		}

		internal static string ToValidFileName(string key)
		{
			return Path.GetInvalidFileNameChars().
				Aggregate(key, (current, c) => current.Replace(c, '_')).
				Replace(' ', '_');
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int GetFileAttributes2(string lpFileName);

		#endregion
	}
}