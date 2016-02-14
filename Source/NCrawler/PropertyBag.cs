using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

using NCrawler.Extensions;

namespace NCrawler
{
	[DataContract]
	public class PropertyBag
	{
		#region Fields

		// A Hashtable to contain the properties in the bag
		private Dictionary<string, Property> _objPropertyCollection = new Dictionary<string, Property>();

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
				if (_objPropertyCollection == null)
				{
					_objPropertyCollection = new Dictionary<string, Property>();
				}

				// An instance of the Property that will be returned
				Property objProperty;

				// If the PropertyBag already contains a property whose name matches
				// the property required, ...
				if (_objPropertyCollection.ContainsKey(name))
				{
					// ... then return the pre-existing property
					objProperty = _objPropertyCollection[name];
				}
				else
				{
					// ... otherwise, create a new Property with a matching name, and
					// a null Value, and add it to the PropertyBag
					objProperty = new Property(name, this);
					_objPropertyCollection.Add(name, objProperty);
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
		public TimeSpan DownloadTime { get; set; }

		[DataMember]
		public WebHeaderCollection Headers { get; internal set; }

		[DataMember]
		public bool IsMutuallyAuthenticated { get; internal set; }

		[DataMember]
		public DateTime LastModified { get; internal set; }

		[DataMember]
		public string Method { get; internal set; }

		[DataMember]
		public Uri OriginalReferrerUrl { get; }

		[DataMember]
		public string OriginalUrl { get; internal set; }

		[DataMember]
		public Version ProtocolVersion { get; internal set; }

		[DataMember]
		public CrawlStep Referrer { get; internal set; }

		[DataMember]
		public byte[] Response { get; set; }

		[DataMember]
		public Uri ResponseUri { get; internal set; }

		[DataMember]
		public string Server { get; internal set; }

		[DataMember]
		public HttpStatusCode StatusCode { get; internal set; }

		[DataMember]
		public string StatusDescription { get; internal set; }

		[DataMember]
		public CrawlStep Step { get; set; }

		[DataMember]
		public string Text { get; set; }

		[DataMember]
		public string Title { get; set; }

		[DataMember]
		public bool StopPipelining { get; set; }

		[DataMember]
		public List<Exception> Exceptions { get; set; } = new List<Exception>();

		[DataMember]
		public string UserAgent { get; set; }

		#endregion

		#region Nested type: Property

		public class Property
		{
			#region Fields

			/// Field to hold the name of the property 
			private object _value;

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
				_value = null;
			}

			#endregion

			#region Instance Properties

			/// <summary>
			/// The name of the Property
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// A pointer to the ultimate client class of the Property / PropertyBag
			/// </summary>
			public object Owner { get; }

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
						return Owner.GetPropertyValue(Name, _value);
					}
				}
				set
				{
					// The lock statement makes the class thread safe. Multiple threads 
					// can attempt to set the value of the Property at the same time
					lock (this)
					{
						_value = value;
						Owner.SetPropertyValue(Name, value);
					}
				}
			}

			#endregion
		}

		#endregion
	}
}