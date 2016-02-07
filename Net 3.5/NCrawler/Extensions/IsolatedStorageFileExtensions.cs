using System.Linq;
using System.IO.IsolatedStorage;

namespace NCrawler.Extensions
{
	public static class IsolatedStorageFileExtensions
	{
		public static bool DirectoryExists(this IsolatedStorageFile store, string directoryName)
		{
			return store.GetDirectoryNames(directoryName).Any();
		}
	}
}
