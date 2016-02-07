using System;
using System.Runtime.InteropServices;

namespace EPocalipse.IFilter
{
	[ComVisible(false)]
	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00000001-0000-0000-C000-000000000046")]
	internal interface IClassFactory
	{
		void CreateInstance([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, ref Guid refiid,
			[MarshalAs(UnmanagedType.Interface)] out object ppunk);

		void LockServer(bool fLock);
	}

	/// <summary>
	/// 	Utility class to get a Class Factory for a certain Class ID 
	/// 	by loading the dll that implements that class
	/// </summary>
	internal static class ComHelper
	{
		#region Class Methods

		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string lpFileName);

		/// <summary>
		/// 	Gets a class factory for a specific COM Class ID.
		/// </summary>
		/// <param name = "dllName">The dll where the COM class is implemented</param>
		/// <param name = "filterPersistClass">The requested Class ID</param>
		/// <returns>IClassFactory instance used to create instances of that class</returns>
		internal static ClassFactoryWrapper GetClassFactory(string dllName, string filterPersistClass)
		{
			//Load the class factory from the dll
			return GetClassFactoryFromDll(dllName, filterPersistClass);
		}

		private static ClassFactoryWrapper GetClassFactoryFromDll(string dllName, string filterPersistClass)
		{
			//Load the dll
			IntPtr dllHandle = LoadLibrary(dllName);
			if (dllHandle == IntPtr.Zero)
			{
				return null;
			}

			//Get a pointer to the DllGetClassObject function
			IntPtr dllGetClassObjectPtr = GetProcAddress(dllHandle, "DllGetClassObject");
			if (dllGetClassObjectPtr == IntPtr.Zero)
			{
				return null;
			}

			//Convert the function pointer to a .net delegate
			DllGetClassObject dllGetClassObject =
				(DllGetClassObject) Marshal.GetDelegateForFunctionPointer(dllGetClassObjectPtr, typeof (DllGetClassObject));

			//Call the DllGetClassObject to retreive a class factory for out Filter class
			Guid filterPersistGuid = new Guid(filterPersistClass);
			Guid classFactoryGuid = new Guid("00000001-0000-0000-C000-000000000046"); //IClassFactory class id
			Object unk;
			if (dllGetClassObject(ref filterPersistGuid, ref classFactoryGuid, out unk) != 0)
			{
				return null;
			}

			IClassFactory result = unk as IClassFactory;
			return result != null ? new ClassFactoryWrapper(dllHandle, result) : null;
		}

		#endregion

		#region Nested type: DllGetClassObject

		private delegate int DllGetClassObject(
			ref Guid classId, ref Guid interfaceId, [Out, MarshalAs(UnmanagedType.Interface)] out object ppunk);

		#endregion
	}

	internal class ClassFactoryWrapper : IDisposable
	{
		#region Readonly & Static Fields

		private readonly IntPtr m_Handle;

		#endregion

		#region Constructors

		public ClassFactoryWrapper(IntPtr handle, IClassFactory classFactory)
		{
			m_Handle = handle;
			ClassFactory = classFactory;
		}

		#endregion

		#region Instance Properties

		public IClassFactory ClassFactory { get; private set; }

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			ComHelper.FreeLibrary(m_Handle);
		}

		#endregion
	}

	internal class FilterWrapper : IDisposable
	{
		#region Readonly & Static Fields

		private readonly ClassFactoryWrapper m_ClassFactoryWrapper;

		#endregion

		#region Constructors

		public FilterWrapper(ClassFactoryWrapper classFactoryWrapper, IFilter filter)
		{
			m_ClassFactoryWrapper = classFactoryWrapper;
			Filter = filter;
		}

		#endregion

		#region Instance Properties

		public IFilter Filter { get; private set; }

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Marshal.ReleaseComObject(Filter);
			m_ClassFactoryWrapper.Dispose();
		}

		#endregion
	}
}