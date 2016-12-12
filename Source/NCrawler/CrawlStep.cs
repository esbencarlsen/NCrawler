using System;
using System.Runtime.Serialization;

using NCrawler.Extensions;

namespace NCrawler
{
	[DataContract]
	[Serializable]
	public class CrawlStep : IEquatable<CrawlStep>, IComparable<CrawlStep>, IComparable
	{
		public CrawlStep(Uri uri, int depth)
		{
			Uri = uri;
			Depth = depth;
		}

		[DataMember]
		public int Depth { get; }

		[DataMember]
		public Uri Uri { get; }

		public override bool Equals(object other)
		{
			if (other.IsNull())
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			CrawlStep crawlStep = other as CrawlStep;
			if (crawlStep != null)
			{
				return Equals(crawlStep);
			}

			return false;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = Depth;
				result = (result*397) ^ (Uri?.GetHashCode() ?? 0);
				return result;
			}
		}

		public override string ToString()
		{
			return $"Depth: {Depth}, Uri: {Uri}";
		}

		public static bool operator ==(CrawlStep left, CrawlStep right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(CrawlStep left, CrawlStep right)
		{
			return !Equals(left, right);
		}

		public int CompareTo(object obj)
		{
			return CompareTo(obj as CrawlStep);
		}

		public int CompareTo(CrawlStep other)
		{
			return string.Compare(Uri.ToString(), other.Uri.ToString(), StringComparison.Ordinal);
		}

		public bool Equals(CrawlStep other)
		{
			if (other.IsNull())
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other.Uri, Uri);
		}
	}
}