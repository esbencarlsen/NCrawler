using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using NCrawler.Utils;

namespace NCrawler.Extensions
{
	public static class AspectExtensions
	{
		#region Class Methods

		[DebuggerStepThrough]
		public static AspectF Delay(this AspectF aspect, int milliseconds)
		{
			return aspect.Combine(work =>
				{
					Thread.Sleep(milliseconds);
					work();
				});
		}

		[DebuggerStepThrough]
		public static AspectF IgnoreException<T>(this AspectF aspect) where T : Exception
		{
			return aspect.Combine(work =>
				{
					try
					{
						work();
					}
					catch (T)
					{
					}
				});
		}

		[DebuggerStepThrough]
		public static AspectF IgnoreExceptions(this AspectF aspect)
		{
			return aspect.Combine(work =>
				{
					try
					{
						work();
					}
					catch
					{
					}
				});
		}

		[DebuggerStepThrough]
		public static AspectF MustBeNonDefault<T>(this AspectF aspect, params T[] args)
			where T : IComparable
		{
			return aspect.Combine(work =>
				{
					T defaultvalue = default(T);
					for (int i = 0; i < args.Length; i++)
					{
						T arg = args[i];
						if (arg.IsNull() || arg.Equals(defaultvalue))
						{
							throw new ArgumentException($"Parameter at index {i} is null");
						}
					}

					work();
				});
		}

		[DebuggerStepThrough]
		public static AspectF MustBeNonNull(this AspectF aspect, params object[] args)
		{
			return aspect.Combine(work =>
				{
					for (int i = 0; i < args.Length; i++)
					{
						object arg = args[i];
						if (arg.IsNull())
						{
							throw new ArgumentException($"Parameter at index {i} is null");
						}
					}

					work();
				});
		}

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

		[DebuggerStepThrough]
		public static AspectF ReadLock(this AspectF aspect, ReaderWriterLockSlim @lock)
		{
			return aspect.Combine(work =>
				{
					@lock.EnterReadLock();
					try
					{
						work();
					}
					finally
					{
						@lock.ExitReadLock();
					}
				});
		}

		[DebuggerStepThrough]
		public static AspectF ReadLockUpgradable(this AspectF aspect, ReaderWriterLockSlim @lock)
		{
			return aspect.Combine(work =>
				{
					@lock.EnterUpgradeableReadLock();
					try
					{
						work();
					}
					finally
					{
						@lock.ExitUpgradeableReadLock();
					}
				});
		}

		[DebuggerStepThrough]
		public static AspectF Retry(this AspectF aspects)
		{
			return aspects.Combine(work =>
				Retry(TimeSpan.FromSeconds(1), 1, (error, retry) => DoNothing(error), x => DoNothing(), work));
		}

		[DebuggerStepThrough]
		public static AspectF Retry(this AspectF aspects, Action<IEnumerable<Exception>> failHandler)
		{
			return aspects.Combine(work =>
				Retry(TimeSpan.FromSeconds(1), 1, (error, retry) => DoNothing(error), x => DoNothing(), work));
		}

		[DebuggerStepThrough]
		public static AspectF Retry(this AspectF aspects, TimeSpan retryDuration)
		{
			return aspects.Combine(work =>
				Retry(retryDuration, 1, (error, retry) => DoNothing(error), x => DoNothing(), work));
		}

		[DebuggerStepThrough]
		public static AspectF Retry(this AspectF aspects, TimeSpan retryDuration,
			Action<Exception, int> errorHandler)
		{
			return aspects.Combine(work =>
				Retry(retryDuration, 1, errorHandler, x => DoNothing(), work));
		}

		[DebuggerStepThrough]
		public static AspectF Retry(this AspectF aspects, TimeSpan retryDuration,
			int retryCount, Action<Exception, int> errorHandler)
		{
			return aspects.Combine(work =>
				Retry(retryDuration, retryCount, errorHandler, x => DoNothing(), work));
		}

		[DebuggerStepThrough]
		public static AspectF Retry(this AspectF aspects, TimeSpan retryDuration,
			int retryCount, Action<Exception, int> errorHandler, Action<IEnumerable<Exception>> retryFailed)
		{
			return aspects.Combine(work =>
				Retry(retryDuration, retryCount, errorHandler, retryFailed, work));
		}

		[DebuggerStepThrough]
		public static void Retry(TimeSpan retryDuration, int retryCount,
			Action<Exception, int> errorHandler, Action<IEnumerable<Exception>> retryFailed, Action work)
		{
			List<Exception> errors = null;
			int maxRetries = retryCount;
			do
			{
				try
				{
					work();
					return;
				}
				catch (Exception x)
				{
					if (null == errors)
					{
						errors = new List<Exception>();
					}

					errors.Add(x);
					if (!errorHandler.IsNull())
					{
						errorHandler(x, maxRetries - retryCount);
					}

					Thread.Sleep(retryDuration);
				}
			} while (retryCount-- > 0);
			if (!retryFailed.IsNull())
			{
				retryFailed(errors);
			}
		}

		[DebuggerStepThrough]
		public static AspectF RunAsync(this AspectF aspect, Action completeCallback)
		{
			return aspect.Combine(work => work.BeginInvoke(asyncresult =>
				{
					work.EndInvoke(asyncresult);
					completeCallback();
				}, null));
		}

		[DebuggerStepThrough]
		public static AspectF Timer(this AspectF aspect, string title)
		{
			return aspect.Combine(work =>
				{
					Stopwatch start = Stopwatch.StartNew();
					work();
					start.Stop();
					Console.Out.WriteLine("{0}: {1}", title, start.Elapsed);
				});
		}

		[DebuggerStepThrough]
		public static AspectF Until(this AspectF aspect, Func<bool> test)
		{
			return aspect.Combine(work =>
				{
					while (!test()) ;
					work();
				});
		}

		[DebuggerStepThrough]
		public static AspectF WhenTrue(this AspectF aspect, params Func<bool>[] conditions)
		{
			return aspect.Combine(work =>
				{
					if (conditions.Any(condition => !condition()))
					{
						return;
					}

					work();
				});
		}

		[DebuggerStepThrough]
		public static AspectF While(this AspectF aspect, Func<bool> test)
		{
			return aspect.Combine(work =>
				{
					while (test())
					{
						work();
					}
				});
		}

		[DebuggerStepThrough]
		public static AspectF WriteLock(this AspectF aspect, ReaderWriterLockSlim @lock)
		{
			return aspect.Combine(work =>
				{
					@lock.EnterWriteLock();
					try
					{
						work();
					}
					finally
					{
						@lock.ExitWriteLock();
					}
				});
		}

		private static void DoNothing()
		{
		}

		private static void DoNothing(params object[] parameters)
		{
		}

		#endregion
	}
}