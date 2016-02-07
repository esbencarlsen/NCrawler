using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using NCrawler.Interfaces;
using NCrawler.Utils;

namespace NCrawler.Extensions
{
	public static class AspectExtensions
	{
		#region Class Methods

		[DebuggerStepThrough]
		public static AspectF Cache<TReturnType>(this AspectF aspect,
			ICache cacheResolver, string key)
		{
			return aspect.Combine(work => Cache<TReturnType>(aspect, cacheResolver, key, work, cached => cached));
		}

		[DebuggerStepThrough]
		public static AspectF CacheList<TItemType, TListType>(this AspectF aspect,
			ICache cacheResolver, string listCacheKey, Func<TItemType, string> getItemKey)
			where TListType : IList<TItemType>, new()
		{
			return aspect.Combine(work =>
				{
					Func<TListType> workDelegate = (Func<TListType>) aspect.m_WorkDelegate;

					// Replace the actual work delegate with a new delegate so that
					// when the actual work delegate returns a collection, each item
					// in the collection is stored in cache individually.
					Func<TListType> newWorkDelegate = () =>
						{
							TListType collection = workDelegate();
							foreach (TItemType item in collection)
							{
								string key = getItemKey(item);
								cacheResolver.Set(key, item);
							}
							return collection;
						};
					aspect.m_WorkDelegate = newWorkDelegate;

					// Get the collection from cache or real source. If collection is returned
					// from cache, resolve each item in the collection from cache
					Cache<TListType>(aspect, cacheResolver, listCacheKey, work,
						cached =>
							{
								// Get each item from cache. If any of the item is not in cache
								// then discard the whole collection from cache and reload the 
								// collection from source.
								TListType itemList = new TListType();
								foreach (TItemType item in cached)
								{
									object cachedItem = cacheResolver.Get(getItemKey(item));
									if (null != cachedItem)
									{
										itemList.Add((TItemType) cachedItem);
									}
									else
									{
										// One of the item is missing from cache. So, discard the 
										// cached list.
										return default(TListType);
									}
								}

								return itemList;
							});
				});
		}

		[DebuggerStepThrough]
		public static AspectF CacheRetry<TReturnType>(this AspectF aspect,
			ICache cacheResolver,
			string key)
		{
			return aspect.Combine(work =>
				{
					try
					{
						Cache<TReturnType>(aspect, cacheResolver, key, work, cached => cached);
					}
					catch
					{
						Thread.Sleep(1000);

						//Retry
						Cache<TReturnType>(aspect, cacheResolver, key, work, cached => cached);
					}
				});
		}

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
							throw new ArgumentException(string.Format("Parameter at index {0} is null", i));
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
							throw new ArgumentException(string.Format("Parameter at index {0} is null", i));
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

		private static void Cache<TReturnType>(AspectF aspect, ICache cacheResolver,
			string key, Action work, Func<TReturnType, TReturnType> foundInCache)
		{
			object cachedData = cacheResolver.Get(key);
			if (cachedData.IsNull())
			{
				GetListFromSource<TReturnType>(aspect, cacheResolver, key);
			}
			else
			{
				// Give caller a chance to shape the cached item before it is returned
				TReturnType cachedType = foundInCache((TReturnType) cachedData);
				if (cachedType.IsNull())
				{
					GetListFromSource<TReturnType>(aspect, cacheResolver, key);
				}
				else
				{
					aspect.m_WorkDelegate = new Func<TReturnType>(() => cachedType);
				}
			}

			work();
		}

		private static void DoNothing()
		{
		}

		private static void DoNothing(params object[] parameters)
		{
		}

		private static void GetListFromSource<TReturnType>(AspectF aspect, ICache cacheResolver, string key)
		{
			Func<TReturnType> workDelegate = (Func<TReturnType>) aspect.m_WorkDelegate;
			TReturnType realObject = workDelegate();
			cacheResolver.Add(key, realObject);
			workDelegate = () => realObject;
			aspect.m_WorkDelegate = workDelegate;
		}

		#endregion
	}
}