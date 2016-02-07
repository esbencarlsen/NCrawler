using System;
using System.Diagnostics;

using NCrawler.Extensions;
using NCrawler.Utils;

namespace NCrawler.Console.Extensions
{
	public static class AspectExtensions
	{
		#region Class Methods

		[DebuggerStepThrough]
		public static AspectF Between(this AspectF aspect, string parameterName, int actualValue, int fromValue, int toValue)
		{
			if (fromValue > toValue)
			{
				throw new ArgumentOutOfRangeException("fromValue", @"From value cannot be less than to value");
			}

			if (actualValue < fromValue)
			{
				throw new Exception(@"{0}({1}) must be greather or equal to {2}".FormatWith(parameterName, actualValue, fromValue));
			}

			if (actualValue > toValue)
			{
				throw new Exception(@"{0}({1}) must be less or equal to {2}".FormatWith(parameterName, actualValue, toValue));
			}

			return aspect;
		}

		[DebuggerStepThrough]
		public static AspectF GreaterOrEqual(this AspectF aspect, string parameterName, int actualValue, int fromValue)
		{
			if (actualValue < fromValue)
			{
				throw new Exception(@"{0}({1}) must be greather or equal to {2}".FormatWith(parameterName, actualValue, fromValue));
			}

			return aspect;
		}

		#endregion
	}
}