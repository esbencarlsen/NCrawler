using NCrawler.Utils;

namespace NCrawler.Extensions
{
	public static class StringExtensions
	{
		public static bool IsNullOrEmpty(this string source)
		{
			return string.IsNullOrEmpty(source);
		}

		public static string Max(this string source, int maxLength)
		{
			AspectF.Define.
				NotNull(source, "source");

			return source.Length > maxLength ? source.Substring(0, maxLength) : source;
		}
	}
}