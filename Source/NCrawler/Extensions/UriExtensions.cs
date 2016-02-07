using System;

using NCrawler.Utils;

namespace NCrawler.Extensions
{
	public static class UriExtensions
	{
		#region Class Methods

		public static string GetUrlKeyString(this Uri uri, UriComponents uriSensitivity)
		{
			// Get complete url
			string completeUrl = uri.ToString().ToUpperInvariant();

			// Get sensitive part
			string sensitiveUrlPart = uri.GetComponents(uriSensitivity, UriFormat.Unescaped);

			if (sensitiveUrlPart.IsNullOrEmpty())
			{
				return completeUrl;
			}

			return completeUrl.Replace(sensitiveUrlPart.ToUpperInvariant(), sensitiveUrlPart);
		}

		/// <summary>
		/// Checks if url is the same as the base url
		/// </summary>
		/// <param name="uriBase">Base Uri</param>
		/// <param name="uri">url to check</param>
		/// <returns>Returns true if url is not same as base url, else false</returns>
		public static bool IsHostMatch(this Uri uriBase, Uri uri)
		{
			AspectF.Define.
				NotNull(uriBase, "uriBase");

			if (uri.IsNull())
			{
				return false;
			}

			return !uriBase.Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}