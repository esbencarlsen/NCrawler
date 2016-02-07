using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using NCrawler.Extensions;

namespace NCrawler
{
	[DataContract]
	[Serializable]
	public class CrawlerQueueEntry : IEquatable<CrawlerQueueEntry>, IComparable<CrawlerQueueEntry>, IComparable
	{
		#region Instance Properties

		[DataMember]
		public CrawlStep CrawlStep { get; set; }

		[DataMember]
		public Dictionary<string, object> Properties { get; set; }

		[DataMember]
		public CrawlStep Referrer { get; set; }

		#endregion

		#region Instance Methods

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof (CrawlerQueueEntry))
			{
				return false;
			}

			return Equals((CrawlerQueueEntry) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = (CrawlStep != null ? CrawlStep.GetHashCode() : 0);
				result = (result*397) ^ (Properties != null ? Properties.GetHashCode() : 0);
				result = (result*397) ^ (Referrer != null ? Referrer.GetHashCode() : 0);
				return result;
			}
		}

		public override string ToString()
		{
			return "CrawlStep: {0}, Properties: {1}, Referrer: {2}".FormatWith(CrawlStep,
				Properties.Select(d => "{0}:{1}".FormatWith(d.Key, d.Value)).Join("; "), Referrer);
		}

		#endregion

		#region Operators

		public static bool operator ==(CrawlerQueueEntry left, CrawlerQueueEntry right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(CrawlerQueueEntry left, CrawlerQueueEntry right)
		{
			return !Equals(left, right);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return CompareTo((CrawlerQueueEntry) obj);
		}

		#endregion

		#region IComparable<CrawlerQueueEntry> Members

		public int CompareTo(CrawlerQueueEntry other)
		{
			return CrawlStep.CompareTo(other.CrawlStep);
		}

		#endregion

		#region IEquatable<CrawlerQueueEntry> Members

		public bool Equals(CrawlerQueueEntry other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other.CrawlStep, CrawlStep) &&
				Equals(other.Referrer, Referrer) &&
				Properties.Select(d => d.Key).SequenceEqual(other.Properties.Select(d => d.Key)) &&
				Properties.Select(d => d.Value).SequenceEqual(other.Properties.Select(d => d.Value));
		}

		#endregion
	}
}