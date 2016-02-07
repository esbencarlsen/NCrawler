using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;

namespace System
{
	public enum LazyThreadSafetyMode
	{
		None,
		PublicationOnly,
		ExecutionAndPublication
	}


	[Serializable, ComVisible(false),
	 DebuggerDisplay("ThreadSafetyMode={Mode}, IsValueCreated={IsValueCreated}, IsValueFaulted={IsValueFaulted}, Value={ValueForDebugDisplay}"),
	 HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public class Lazy<T>
	{
		// Fields

		#region Readonly & Static Fields

		private static readonly Func<T> s_PublicationOnlyOrAlreadyInitialized;
		[NonSerialized] private readonly object m_ThreadSafeObj;

		#endregion

		#region Fields

		private volatile object m_Boxed;
		[NonSerialized] private Func<T> m_ValueFactory;

		#endregion

		#region Constructors

		static Lazy()
		{
			s_PublicationOnlyOrAlreadyInitialized = delegate { return default(T); };
		}

		public Lazy() : this(LazyThreadSafetyMode.ExecutionAndPublication)
		{
		}

		public Lazy(bool isThreadSafe)
			: this(isThreadSafe ? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None)
		{
		}

		public Lazy(Func<T> valueFactory) : this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication)
		{
		}

		public Lazy(LazyThreadSafetyMode mode)
		{
			m_ThreadSafeObj = GetObjectFromMode(mode);
		}

		public Lazy(Func<T> valueFactory, bool isThreadSafe) :
			this(valueFactory, isThreadSafe ? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None)
		{
		}

		public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
		{
			if (valueFactory == null)
			{
				throw new ArgumentNullException("valueFactory");
			}
			
			m_ThreadSafeObj = GetObjectFromMode(mode);
			m_ValueFactory = valueFactory;
		}

		#endregion

		// Properties

		#region Instance Properties

		public bool IsValueCreated
		{
			get { return ((m_Boxed != null) && (m_Boxed is Boxed)); }
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Value
		{
			get
			{
				if (m_Boxed != null)
				{
					Boxed boxed = m_Boxed as Boxed;
					if (boxed != null)
					{
						return boxed.Value;
					}

					LazyInternalExceptionHolder holder = (LazyInternalExceptionHolder)m_Boxed;
					throw holder.Exception;
				}

				return LazyInitValue();
			}
		}

		internal bool IsValueFaulted
		{
			get { return (m_Boxed is LazyInternalExceptionHolder); }
		}

		internal LazyThreadSafetyMode Mode
		{
			get
			{
				if (m_ThreadSafeObj == null)
				{
					return LazyThreadSafetyMode.None;
				}

				if (Equals(m_ThreadSafeObj, s_PublicationOnlyOrAlreadyInitialized))
				{
					return LazyThreadSafetyMode.PublicationOnly;
				}

				return LazyThreadSafetyMode.ExecutionAndPublication;
			}
		}

		internal T ValueForDebugDisplay
		{
			get
			{
				if (!IsValueCreated)
				{
					return default(T);
				}

				return ((Boxed) m_Boxed).Value;
			}
		}

		#endregion

		#region Instance Methods

		public override string ToString()
		{
			if (!IsValueCreated)
			{
				return "Lazy_ToString_ValueNotCreated";
			}

			return Value.ToString();
		}

		private Boxed CreateValue()
		{
			Boxed boxed;
			LazyThreadSafetyMode mode = Mode;
			if (m_ValueFactory != null)
			{
				try
				{
					if ((mode != LazyThreadSafetyMode.PublicationOnly) &&
						(m_ValueFactory == s_PublicationOnlyOrAlreadyInitialized))
					{
						throw new InvalidOperationException("Lazy_Value_RecursiveCallsToValue");
					}
					Func<T> valueFactory = m_ValueFactory;
					if (mode != LazyThreadSafetyMode.PublicationOnly)
					{
						m_ValueFactory = s_PublicationOnlyOrAlreadyInitialized;
					}
					return new Boxed(valueFactory());
				}
				catch (Exception exception)
				{
					if (mode != LazyThreadSafetyMode.PublicationOnly)
					{
						m_Boxed = new LazyInternalExceptionHolder(exception);
					}

					throw;
				}
			}
			try
			{
				boxed = new Boxed((T) Activator.CreateInstance(typeof (T)));
			}
			catch (MissingMethodException)
			{
				Exception ex = new MissingMemberException("Lazy_CreateValue_NoParameterlessCtorForT");
				if (mode != LazyThreadSafetyMode.PublicationOnly)
				{
					m_Boxed = new LazyInternalExceptionHolder(ex);
				}

				throw ex;
			}

			return boxed;
		}

		private T LazyInitValue()
		{
			Boxed boxed;
			switch (Mode)
			{
				case LazyThreadSafetyMode.None:
					boxed = CreateValue();
					m_Boxed = boxed;
					break;
                                       
				case LazyThreadSafetyMode.PublicationOnly:
					boxed = CreateValue();
					object mBoxed = m_Boxed;
					if (Interlocked.CompareExchange(ref mBoxed, boxed, null) != null)
					{
						boxed = (Boxed) m_Boxed;
					}

					break;
				default:
					{
						object obj2;
						bool lockTaken = Monitor.TryEnter(obj2 = m_ThreadSafeObj);
						try
						{
							if (m_Boxed == null)
							{
								boxed = CreateValue();
								m_Boxed = boxed;
							}
							else
							{
								boxed = m_Boxed as Boxed;
								if (boxed == null)
								{
									LazyInternalExceptionHolder holder = (LazyInternalExceptionHolder)m_Boxed;
									throw holder.Exception;
								}
							}
						}
						finally
						{
							if (lockTaken)
							{
								Monitor.Exit(obj2);
							}
						}
						break;
					}
			}
			
			return boxed.Value;
		}

		#endregion

		#region Class Methods

		private static object GetObjectFromMode(LazyThreadSafetyMode mode)
		{
			if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
			{
				return new object();
			}
			
			if (mode == LazyThreadSafetyMode.PublicationOnly)
			{
				return s_PublicationOnlyOrAlreadyInitialized;
			}
			
			if (mode != LazyThreadSafetyMode.None)
			{
				throw new ArgumentOutOfRangeException("mode", "Lazy_ctor_ModeInvalid");
			}

			return null;
		}

		#endregion

		#region Nested type: Boxed

		[Serializable]
		private class Boxed
		{
			#region Readonly & Static Fields

			internal readonly T Value;

			#endregion

			#region Constructors

			internal Boxed(T value)
			{
				Value = value;
			}

			#endregion
		}

		#endregion

		#region Nested type: LazyInternalExceptionHolder

		private class LazyInternalExceptionHolder
		{
			#region Readonly & Static Fields

			internal readonly Exception Exception;

			#endregion

			#region Constructors

			internal LazyInternalExceptionHolder(Exception ex)
			{
				Exception = ex;
			}

			#endregion
		}

		#endregion
	}
}