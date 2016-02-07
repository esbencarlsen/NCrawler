using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

using NCrawler.Extensions;

namespace NCrawler
{
	[DataContract]
	public class PropertyBag : IEquatable<PropertyBag>, IComparable<PropertyBag>, IComparable
	{
		#region Fields

		// A Hashtable to contain the properties in the bag
		private Dictionary<string, Property> m_ObjPropertyCollection = new Dictionary<string, Property>();

		#endregion

		#region Instance Indexers

		/// <summary>
		/// Indexer which retrieves a property from the PropertyBag based on 
		/// the property name
		/// </summary>
		public Property this[string name]
		{
			get
			{
				if (m_ObjPropertyCollection == null)
				{
					m_ObjPropertyCollection = new Dictionary<string, Property>();
				}

				// An instance of the Property that will be returned
				Property objProperty;

				// If the PropertyBag already contains a property whose name matches
				// the property required, ...
				if (m_ObjPropertyCollection.ContainsKey(name))
				{
					// ... then return the pre-existing property
					objProperty = m_ObjPropertyCollection[name];
				}
				else
				{
					// ... otherwise, create a new Property with a matching name, and
					// a null Value, and add it to the PropertyBag
					objProperty = new Property(name, this);
					m_ObjPropertyCollection.Add(name, objProperty);
				}

				return objProperty;
			}
		}

		#endregion

		#region Instance Properties

		[DataMember]
		public string CharacterSet { get; internal set; }

		[DataMember]
		public string ContentEncoding { get; internal set; }

		[DataMember]
		public string ContentType { get; internal set; }

		[DataMember]
		public TimeSpan DownloadTime { get; internal set; }

		[DataMember]
		public WebHeaderCollection Headers { get; internal set; }

		[DataMember]
		public bool IsFromCache { get; internal set; }

		[DataMember]
		public bool IsMutuallyAuthenticated { get; internal set; }

		[DataMember]
		public DateTime LastModified { get; internal set; }

		[DataMember]
		public string Method { get; internal set; }

		[DataMember]
		public Uri OriginalReferrerUrl { get; internal set; }

		[DataMember]
		public string OriginalUrl { get; internal set; }

		[DataMember]
		public Version ProtocolVersion { get; internal set; }

		[DataMember]
		public CrawlStep Referrer { get; internal set; }

		[DataMember]
		public Func<Stream> GetResponse { get; set; }

		[DataMember]
		public Uri ResponseUri { get; internal set; }

		[DataMember]
		public string Server { get; internal set; }

		[DataMember]
		public HttpStatusCode StatusCode { get; internal set; }

		[DataMember]
		public string StatusDescription { get; internal set; }

		[DataMember]
		public CrawlStep Step { get; internal set; }

		[DataMember]
		public string Text { get; set; }

		[DataMember]
		public string Title { get; set; }

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

			if (obj.GetType() != typeof (PropertyBag))
			{
				return false;
			}

			return Equals((PropertyBag) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = (m_ObjPropertyCollection != null ? m_ObjPropertyCollection.GetHashCode() : 0);
				result = (result*397) ^ (CharacterSet != null ? CharacterSet.GetHashCode() : 0);
				result = (result*397) ^ (ContentEncoding != null ? ContentEncoding.GetHashCode() : 0);
				result = (result*397) ^ (ContentType != null ? ContentType.GetHashCode() : 0);
				result = (result*397) ^ (Headers != null ? Headers.GetHashCode() : 0);
				result = (result*397) ^ IsFromCache.GetHashCode();
				result = (result*397) ^ IsMutuallyAuthenticated.GetHashCode();
				result = (result*397) ^ LastModified.GetHashCode();
				result = (result*397) ^ (Method != null ? Method.GetHashCode() : 0);
				result = (result*397) ^ (OriginalReferrerUrl != null ? OriginalReferrerUrl.GetHashCode() : 0);
				result = (result*397) ^ (OriginalUrl != null ? OriginalUrl.GetHashCode() : 0);
				result = (result*397) ^ (ProtocolVersion != null ? ProtocolVersion.GetHashCode() : 0);
				result = (result*397) ^ (Referrer != null ? Referrer.GetHashCode() : 0);
				result = (result*397) ^ (ResponseUri != null ? ResponseUri.GetHashCode() : 0);
				result = (result*397) ^ (Server != null ? Server.GetHashCode() : 0);
				result = (result*397) ^ StatusCode.GetHashCode();
				result = (result*397) ^ (StatusDescription != null ? StatusDescription.GetHashCode() : 0);
				result = (result*397) ^ (Step != null ? Step.GetHashCode() : 0);
				result = (result*397) ^ (Text != null ? Text.GetHashCode() : 0);
				result = (result*397) ^ (Title != null ? Title.GetHashCode() : 0);
				result = (result*397) ^ DownloadTime.GetHashCode();
				return result;
			}
		}

		#endregion

		#region Operators

		public static bool operator ==(PropertyBag left, PropertyBag right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(PropertyBag left, PropertyBag right)
		{
			return !Equals(left, right);
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			return CompareTo(obj as PropertyBag);
		}

		#endregion

		#region IComparable<PropertyBag> Members

		public int CompareTo(PropertyBag other)
		{
			return Step.CompareTo(other.Step);
		}

		#endregion

		#region IEquatable<PropertyBag> Members

		public bool Equals(PropertyBag other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other.m_ObjPropertyCollection, m_ObjPropertyCollection) &&
				Equals(other.CharacterSet, CharacterSet) &&
				Equals(other.ContentEncoding, ContentEncoding) &&
				Equals(other.ContentType, ContentType) &&
				Equals(other.Headers, Headers) &&
				other.IsFromCache.Equals(IsFromCache) &&
				other.IsMutuallyAuthenticated.Equals(IsMutuallyAuthenticated) &&
				other.LastModified.Equals(LastModified) &&
				Equals(other.Method, Method) &&
				Equals(other.OriginalReferrerUrl, OriginalReferrerUrl) &&
				Equals(other.OriginalUrl, OriginalUrl) &&
				Equals(other.ProtocolVersion, ProtocolVersion) &&
				Equals(other.Referrer, Referrer) &&
				Equals(other.ResponseUri, ResponseUri) &&
				Equals(other.Server, Server) &&
				Equals(other.StatusCode, StatusCode) &&
				Equals(other.StatusDescription, StatusDescription) &&
				Equals(other.Step, Step) &&
				Equals(other.Text, Text) &&
				Equals(other.Title, Title) &&
				other.DownloadTime.Equals(DownloadTime);
		}

		#endregion

		#region Nested type: Property

		public class Property
		{
			#region Fields

			/// Field to hold the name of the property 
			private object m_Value;

			#endregion

			#region Constructors

			/// Event fires immediately prior to value changes
			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="name">The name of the new property</param>
			/// <param name="owner">The owner i.e. parent of the PropertyBag</param>
			public Property(string name, object owner)
			{
				Name = name;
				Owner = owner;
				m_Value = null;
			}

			#endregion

			#region Instance Properties

			/// <summary>
			/// The name of the Property
			/// </summary>
			public string Name { get; private set; }

			/// <summary>
			/// A pointer to the ultimate client class of the Property / PropertyBag
			/// </summary>
			public object Owner { get; private set; }

			/// <summary>
			/// The property value
			/// </summary>
			public object Value
			{
				get
				{
					// The lock statement makes the class thread safe. Multiple threads 
					// can attempt to get the value of the Property at the same time
					lock (this)
					{
						return Owner.GetPropertyValue(Name, m_Value);
					}
				}
				set
				{
					// The lock statement makes the class thread safe. Multiple threads 
					// can attempt to set the value of the Property at the same time
					lock (this)
					{
						m_Value = value;
						Owner.SetPropertyValue(Name, value);
					}
				}
			}

			#endregion
		}

		#endregion
	}
}