using System;
using System.Diagnostics;

using NCrawler.Utils;

namespace NCrawler.Extensions
{
	public static class AspectExtensions
	{
		[DebuggerStepThrough]
		public static AspectF NotNull(this AspectF aspect, object @object, string parameterName)
		{
			if (@object.IsNull())
			{
				throw new ArgumentNullException(parameterName);
			}

			return aspect;
		}

		[DebuggerStepThrough]
		public static AspectF NotNullOrEmpty(this AspectF aspect, string @object, string parameterName)
		{
			if (@object.IsNullOrEmpty())
			{
				throw new ArgumentNullException(parameterName);
			}

			return aspect;
		}
	}
}