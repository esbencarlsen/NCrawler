using System;
using System.Diagnostics;

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
				throw new ArgumentOutOfRangeException(nameof(fromValue), @"From value cannot be less than to value");
			}

			if (actualValue < fromValue)
			{
				throw new Exception($"{parameterName}({actualValue}) must be greather or equal to {fromValue}");
			}

			if (actualValue > toValue)
			{
				throw new Exception($"{parameterName}({actualValue}) must be less or equal to {toValue}");
			}

			return aspect;
		}

		[DebuggerStepThrough]
		public static AspectF GreaterOrEqual(this AspectF aspect, string parameterName, int actualValue, int fromValue)
		{
			if (actualValue < fromValue)
			{
				throw new Exception(@"{parameterName}({actualValue}) must be greather or equal to {fromValue}");
			}

			return aspect;
		}

		#endregion
	}
}