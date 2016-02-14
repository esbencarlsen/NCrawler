using System;
using System.Linq;
using System.Reflection;

namespace NCrawler.Extensions
{
	public static class ObjectExtensions
	{
		/// <summary>
		///     Dynamically retrieves a property value.
		/// </summary>
		/// <typeparam name="T">The expected return data type</typeparam>
		/// <param name="obj">The object to perform on.</param>
		/// <param name="propertyName">The Name of the property.</param>
		/// <param name="defaultValue">The default value to return.</param>
		/// <returns>The property value.</returns>
		/// <example>
		///     <code>
		/// var type = Type.GetType("System.IO.FileInfo, mscorlib");
		/// var file = type.CreateInstance(@"c:\autoexec.bat");
		/// if(file.GetPropertyValue&lt;bool&gt;("Exists")) {
		///  var reader = file.InvokeMethod&lt;StreamReader&gt;("OpenText");
		///  Console.WriteLine(reader.ReadToEnd());
		///  reader.Close();
		/// }
		/// </code>
		/// </example>
		public static T GetPropertyValue<T>(this object obj, string propertyName, T defaultValue)
		{
			Type type = obj.GetType();
			PropertyInfo property = type.GetProperty(propertyName);

			if (property.IsNull())
			{
				return defaultValue;
			}

			object value = property.GetValue(obj, null);
			return value is T ? (T) value : defaultValue;
		}

		/// <summary>
		///     Dynamically sets a property value.
		/// </summary>
		/// <param name="obj">The object to perform on.</param>
		/// <param name="propertyName">The Name of the property.</param>
		/// <param name="value">The value to be set.</param>
		public static void SetPropertyValue(this object obj, string propertyName, object value)
		{
			Type type = obj.GetType();
			PropertyInfo property = type.GetProperty(propertyName);

			if (!property.IsNull())
			{
				property.SetValue(obj, value, null);
			}
		}

		public static bool IsNull<T>(this T @object)
		{
			return Equals(@object, null);
		}

		public static bool IsIn<T>(this T t, params T[] tt)
		{
			return tt.Contains(t);
		}
	}
}