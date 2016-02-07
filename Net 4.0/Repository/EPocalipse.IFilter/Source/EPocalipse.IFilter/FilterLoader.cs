using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

using Microsoft.Win32;

namespace EPocalipse.IFilter
{
	/// <summary>
	/// 	FilterLoader finds the dll and ClassID of the COM object responsible  
	/// 	for filtering a specific file extension. 
	/// 	It then loads that dll, creates the appropriate COM object and returns 
	/// 	a pointer to an IFilter instance
	/// </summary>
	internal static class FilterLoader
	{
		#region Readonly & Static Fields

		private static readonly Dictionary<string, CacheEntry> s_Cache = new Dictionary<string, CacheEntry>();

		#endregion

		#region Class Methods

		internal static FilterWrapper LoadAndInitIFilter(string fileName)
		{
			return LoadAndInitIFilter(fileName, Path.GetExtension(fileName));
		}

		internal static FilterWrapper LoadAndInitIFilter(string fileName, string extension)
		{
			FilterWrapper filter = LoadIFilter(extension);
			if (filter == null)
			{
				return null;
			}

			IPersistFile persistFile = (filter.Filter as IPersistFile);
			if (persistFile != null)
			{
				persistFile.Load(fileName, 0);
				IFILTER_FLAGS flags;
				const IFILTER_INIT iflags = IFILTER_INIT.CANON_HYPHENS |
					IFILTER_INIT.CANON_PARAGRAPHS |
					IFILTER_INIT.CANON_SPACES |
					IFILTER_INIT.APPLY_INDEX_ATTRIBUTES |
					IFILTER_INIT.HARD_LINE_BREAKS |
					IFILTER_INIT.FILTER_OWNED_VALUE_OK;

				if (filter.Filter.Init(iflags, 0, IntPtr.Zero, out flags) == IFilterReturnCode.S_OK)
				{
					return filter;
				}
			}

			using (filter)
			return null;
		}

		private static void AddExtensionToCache(string ext, string dllName, string filterPersistClass)
		{
			lock (s_Cache)
			{
				s_Cache.Add(ext.ToLower(), new CacheEntry(dllName, filterPersistClass));
			}
		}

		private static bool GetFilterDllAndClass(string ext, out string dllName, out string filterPersistClass)
		{
			if (!GetFilterDllAndClassFromCache(ext, out dllName, out filterPersistClass))
			{
				string persistentHandlerClass = GetPersistentHandlerClass(ext, true);
				if (persistentHandlerClass != null)
				{
					GetFilterDllAndClassFromPersistentHandler(persistentHandlerClass,
						out dllName, out filterPersistClass);
				}

				AddExtensionToCache(ext, dllName, filterPersistClass);
			}

			return (dllName != null && filterPersistClass != null);
		}

		private static bool GetFilterDllAndClassFromCache(string ext, out string dllName, out string filterPersistClass)
		{
			string lowerExt = ext.ToLower();
			lock (s_Cache)
			{
				CacheEntry cacheEntry;
				if (s_Cache.TryGetValue(lowerExt, out cacheEntry))
				{
					dllName = cacheEntry.DllName;
					filterPersistClass = cacheEntry.ClassName;
					return true;
				}
			}

			dllName = null;
			filterPersistClass = null;
			return false;
		}

		private static void GetFilterDllAndClassFromPersistentHandler(string persistentHandlerClass, out string dllName, out string filterPersistClass)
		{
			dllName = null;

			//Read the CLASS ID of the IFilter persistent handler
			filterPersistClass = ReadStrFromHklm(string.Format(@"Software\Classes\CLSID\{0}\PersistentAddinsRegistered\{{89BCB740-6119-101A-BCB7-00DD010655AF}}", persistentHandlerClass));
			if (string.IsNullOrEmpty(filterPersistClass))
			{
				return;
			}

			//Read the dll name 
			dllName = ReadStrFromHklm(string.Format(@"Software\Classes\CLSID\{0}\InprocServer32", filterPersistClass));
		}

		private static string GetPersistentHandlerClass(string ext, bool searchContentType)
		{
			//Try getting the info from the file extension
			string persistentHandlerClass = GetPersistentHandlerClassFromExtension(ext);
			if (string.IsNullOrEmpty(persistentHandlerClass))
			{
				//try getting the info from the document type 
				persistentHandlerClass = GetPersistentHandlerClassFromDocumentType(ext);
			}
			
			if (searchContentType && string.IsNullOrEmpty(persistentHandlerClass))
			{
				//Try getting the info from the Content Type
				persistentHandlerClass = GetPersistentHandlerClassFromContentType(ext);
			}

			return persistentHandlerClass;
		}

		private static string GetPersistentHandlerClassFromContentType(string ext)
		{
			string contentType = ReadStrFromHklm(string.Format(@"Software\Classes\{0}", ext), "Content Type");
			if (string.IsNullOrEmpty(contentType))
			{
				return null;
			}

			string contentTypeExtension = ReadStrFromHklm(string.Format(@"Software\Classes\MIME\Database\Content Type\{0}", contentType),
				"Extension");
			if (ext.Equals(contentTypeExtension, StringComparison.CurrentCultureIgnoreCase))
			{
				return null; //No need to look further. This extension does not have any persistent handler
			}

			//We know the extension that is assciated with that content type. Simply try again with the new extension
			return GetPersistentHandlerClass(contentTypeExtension, false); //Don't search content type this time.
		}

		private static string GetPersistentHandlerClassFromDocumentType(string ext)
		{
			//Get the DocumentType of this file extension
			string docType = ReadStrFromHklm(string.Format(@"Software\Classes\{0}", ext));
			if (string.IsNullOrEmpty(docType))
			{
				return null;
			}

			//Get the Class ID for this document type
			string docClass = ReadStrFromHklm(string.Format(@"Software\Classes\{0}\CLSID", docType));
			if (string.IsNullOrEmpty(docType))
			{
				return null;
			}

			//Now get the PersistentHandler for that Class ID
			return ReadStrFromHklm(string.Format(@"Software\Classes\CLSID\{0}\PersistentHandler", docClass));
		}

		private static string GetPersistentHandlerClassFromExtension(string ext)
		{
			return ReadStrFromHklm(string.Format(@"Software\Classes\{0}\PersistentHandler", ext));
		}

		private static FilterWrapper LoadFilterFromDll(string dllName, string filterPersistClass)
		{
			//Get a classFactory for our classID
			ClassFactoryWrapper classFactory = ComHelper.GetClassFactory(dllName, filterPersistClass);
			if (classFactory == null)
			{
				return null;
			}

			object obj;
			//And create an IFilter instance using that class factory
			Guid filterGuid = new Guid("89BCB740-6119-101A-BCB7-00DD010655AF");
			classFactory.ClassFactory.CreateInstance(null, ref filterGuid, out obj);

			if (obj as IFilter == null)
			{
				using (classFactory)
				return null;
			}

			return new FilterWrapper(classFactory, obj as IFilter);
		}

		/// <summary>
		/// 	finds an IFilter implementation for a file type
		/// </summary>
		/// <param name = "ext">The extension of the file</param>
		/// <returns>an IFilter instance used to retreive text from that file type</returns>
		private static FilterWrapper LoadIFilter(string ext)
		{
			string dllName, filterPersistClass;

			//Find the dll and ClassID
			return GetFilterDllAndClass(ext, out dllName, out filterPersistClass)
				? LoadFilterFromDll(dllName, filterPersistClass)
				: null;
		}

		private static string ReadStrFromHklm(string key, string value = null)
		{
			RegistryKey rk = Registry.LocalMachine.OpenSubKey(key);
			if (rk == null)
			{
				return null;
			}

			using (rk)
			{
				return (string) rk.GetValue(value);
			}
		}

		#endregion

		#region Nested type: CacheEntry

		private class CacheEntry
		{
			#region Readonly & Static Fields

			public string ClassName { get; private set; }
			public string DllName { get; private set; }

			#endregion

			#region Constructors

			public CacheEntry(string dllName, string className)
			{
				DllName = dllName;
				ClassName = className;
			}

			#endregion
		}

		#endregion
	}
}