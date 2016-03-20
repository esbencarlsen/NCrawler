using System;
using System.Diagnostics;

using NCrawler.Extensions;

namespace NCrawler.Utils
{
	public class AspectF
	{
		#region Fields

		/// <summary>
		/// Chain of aspects to invoke
		/// </summary>
		internal Action<Action> Chain;

		/// <summary>
		/// The acrual work delegate that is finally called
		/// </summary>
		internal Delegate WorkDelegate;

		#endregion

		#region Instance Methods

		/// <summary>
		/// Create a composition of function e.g. f(g(x))
		/// </summary>
		/// <param name="newAspectDelegate">A delegate that offers an aspect's behavior. 
		/// It's added into the aspect chain</param>
		/// <returns></returns>
		[DebuggerStepThrough]
		public AspectF Combine(Action<Action> newAspectDelegate)
		{
			if (Chain.IsNull())
			{
				Chain = newAspectDelegate;
			}
			else
			{
				Action<Action> existingChain = Chain;
				Action<Action> callAnother = work => existingChain(() => newAspectDelegate(work));
				Chain = callAnother;
			}

			return this;
		}

		/// <summary>
		/// Execute your real code applying the aspects over it
		/// </summary>
		/// <param name="work">The actual code that needs to be run</param>
		[DebuggerStepThrough]
		public void Do(Action work)
		{
			if (Chain.IsNull())
			{
				work();
			}
			else
			{
				Chain(work);
			}
		}

		/// <summary>
		/// Execute your real code applying the aspects over it
		/// </summary>
		/// <param name="work">The actual code that needs to be run</param>
		[DebuggerStepThrough]
		public void Do<TParam1>(Action<TParam1> work) where TParam1 : IDisposable, new()
		{
			using (TParam1 p = new TParam1())
			{
				Do(p, work);
			}
		}

		/// <summary>
		/// Execute your real code applying the aspects over it
		/// </summary>
		/// <param name="p"></param>
		/// <param name="work">
		/// 	The actual code that needs to be run
		/// </param>
		[DebuggerStepThrough]
		public void Do<TParam1>(TParam1 p, Action<TParam1> work)
		{
			if (Chain.IsNull())
			{
				work(p);
			}
			else
			{
				Chain(() => work(p));
			}
		}

		/// <summary>
		/// Execute your real code applying aspects over it.
		/// </summary>
		/// <typeparam name="TReturnType"></typeparam>
		/// <param name="work">The actual code that needs to be run</param>
		/// <returns></returns>
		[DebuggerStepThrough]
		public TReturnType Return<TReturnType>(Func<TReturnType> work)
		{
			WorkDelegate = work;

			if (Chain.IsNull())
			{
				return work();
			}

			TReturnType returnValue = default(TReturnType);
			Chain(() =>
			{
				Func<TReturnType> workDelegate = (Func<TReturnType>)WorkDelegate;
				returnValue = workDelegate();
			});
			return returnValue;
		}

		/// <summary>
		/// Execute your real code applying aspects over it.
		/// </summary>
		/// <typeparam name="TReturnType"></typeparam>
		/// <typeparam name="TParam1"></typeparam>
		/// <param name="work">The actual code that needs to be run</param>
		/// <returns></returns>
		[DebuggerStepThrough]
		public TReturnType Return<TReturnType, TParam1>(Func<TParam1, TReturnType> work) where TParam1 : IDisposable, new()
		{
			using (TParam1 p = new TParam1())
			{
				return Return(p, work);
			}
		}

		/// <summary>
		/// Execute your real code applying aspects over it.
		/// </summary>
		/// <typeparam name="TReturnType"></typeparam>
		/// <typeparam name="TParam1"></typeparam>
		/// <param name="p"></param>
		/// <param name="work">
		/// 	The actual code that needs to be run
		/// </param>
		/// <returns></returns>
		[DebuggerStepThrough]
		public TReturnType Return<TReturnType, TParam1>(TParam1 p, Func<TParam1, TReturnType> work)
		{
			WorkDelegate = work;

			if (Chain.IsNull())
			{
				return work(p);
			}

			TReturnType returnValue = default(TReturnType);
			Chain(() =>
			{
				Func<TParam1, TReturnType> workDelegate = (Func<TParam1, TReturnType>)WorkDelegate;
				returnValue = workDelegate(p);
			});

			return returnValue;
		}

		#endregion

		#region Class Properties

		/// <summary>
		/// Handy property to start writing aspects using fluent style
		/// </summary>
		public static AspectF Define
		{
			[DebuggerStepThrough]
			get { return new AspectF(); }
		}

		#endregion
	}
}