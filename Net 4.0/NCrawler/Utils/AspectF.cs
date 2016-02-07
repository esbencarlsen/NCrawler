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
		internal Action<Action> m_Chain;

		/// <summary>
		/// The acrual work delegate that is finally called
		/// </summary>
		internal Delegate m_WorkDelegate;

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
			if (m_Chain.IsNull())
			{
				m_Chain = newAspectDelegate;
			}
			else
			{
				Action<Action> existingChain = m_Chain;
				Action<Action> callAnother = work => existingChain(() => newAspectDelegate(work));
				m_Chain = callAnother;
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
			if (m_Chain.IsNull())
			{
				work();
			}
			else
			{
				m_Chain(work);
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
			if (m_Chain.IsNull())
			{
				work(p);
			}
			else
			{
				m_Chain(() => work(p));
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
			m_WorkDelegate = work;

			if (m_Chain.IsNull())
			{
				return work();
			}

			TReturnType returnValue = default(TReturnType);
			m_Chain(() =>
			{
				Func<TReturnType> workDelegate = (Func<TReturnType>)m_WorkDelegate;
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
			m_WorkDelegate = work;

			if (m_Chain.IsNull())
			{
				return work(p);
			}

			TReturnType returnValue = default(TReturnType);
			m_Chain(() =>
			{
				Func<TParam1, TReturnType> workDelegate = (Func<TParam1, TReturnType>)m_WorkDelegate;
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