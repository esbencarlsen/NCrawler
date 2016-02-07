using System;
using System.Runtime.Serialization;

using NCrawler.Extensions;

namespace NCrawler
{
	[DataContract]
	[Serializable]
	public class CrawlStep : IEquatable<CrawlStep>, IComparable<CrawlStep>, IComparable
	{
		#region Constructors

		public CrawlStep(Uri uri, int depth)
		{
			Uri = uri;
			Depth = depth;
			IsAllowed = true;
			IsExternalUrl = false;
		}

		#endregion

		#region Instance Properties

		[DataMember]
		public int Depth { get; private set; }

		[DataMember]
		public bool IsAllowed { get; set; }

		[DataMember]
		public bool IsExternalUrl { get; set; }

		[DataMember]
		public Uri Uri { get; internal set; }

		#endregion

		#region Instance Methods

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

			if (other is CrawlStep)
			{
				return Equals((CrawlStep)other);
			}

			return false;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = Depth;
				result = (result*397) ^ IsAllowed.GetHashCode();
				result = (result*397) ^ IsExternalUrl.GetHashCode();
				result = (result*397) ^ (Uri != null ? Uri.GetHashCode() : 0);
				return result;
			}
		}

		public override string ToString()
		{
			return "Depth: {0}, IsAllowed: {1}, IsExternalUrl: {2}, Uri: {3}".FormatWith(Depth, IsAllowed, IsExternalUrl, Uri);
		}

		#endregion

		#region Operators

		public static bool operator ==(CrawlStep left, CrawlStep right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(CrawlStep left, CrawlStep right)
		{
			return !Equals(left, right);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return CompareTo(obj as CrawlStep);
		}

		#endregion

		#region IComparable<CrawlStep> Members

		public int CompareTo(CrawlStep other)
		{
			return Uri.ToString().CompareTo(other.Uri.ToString());
		}

		#endregion

		#region IEquatable<CrawlStep> Members

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

		#endregion
	}
}