using System.Diagnostics;

using NCrawler.Extensions;
using NCrawler.Interfaces;

namespace NCrawler.Services
{
	public class SystemTraceLoggerService : ILog
	{
		#region ILog Members

		public void Verbose(string format, params object[] parameters)
		{
			Trace.TraceInformation(ToMessage(format, parameters));
		}

		public void Warning(string format, params object[] parameters)
		{
			Trace.TraceWarning(ToMessage(format, parameters));
		}

		public void Debug(string format, params object[] parameters)
		{
			System.Diagnostics.Debug.Write(ToMessage(format, parameters));
		}

		public void Error(string format, params object[] parameters)
		{
			Trace.TraceError(ToMessage(format, parameters));
		}

		public void FatalError(string format, params object[] parameters)
		{
			Trace.TraceError(ToMessage(format, parameters));
		}

		#endregion

		#region Class Methods

		private static string ToMessage(string format, object[] parameters)
		{
			return format.FormatWith(parameters);
		}

		#endregion
	}
}