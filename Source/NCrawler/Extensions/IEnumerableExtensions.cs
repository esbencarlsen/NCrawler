using System;
using System.Collections.Generic;
using System.Linq;

namespace NCrawler.Extensions
{
	public static class EnumerableExtensions
	{
		public static string Join<T>(this IEnumerable<T> target, string separator)
		{
			return target.IsNull()
				? string.Empty
				: string.Join(separator, target.Select(i => i.ToString()).ToArray());
		}

		/// <summary>
		/// Adds one or more elements to sequence
		/// </summary>
		/// <typeparam name="T">Sequence element type</typeparam>
		/// <param name="target">Initial sequence</param>
		/// <param name="values">Elements to concat</param>
		/// <returns>United sequences</returns>
		public static IEnumerable<T> AddToEnd<T>(this IEnumerable<T> target, params T[] values)
		{
			if (target != null)
			{
				foreach (T item in target)
				{
					yield return item;
				}
			}

			if (values != null)
			{
				foreach (T value in values)
				{
					yield return value;
				}
			}
		}
	}
}