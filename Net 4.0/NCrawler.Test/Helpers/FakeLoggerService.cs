using NCrawler.Interfaces;

namespace NCrawler.Test.Helpers
{
	public class FakeLoggerService : ILog
	{
		#region ILog Members

		public void Verbose(string format, params object[] parameters)
		{
		}

		public void Warning(string format, params object[] parameters)
		{
		}

		public void Debug(string format, params object[] parameters)
		{
		}

		public void Error(string format, params object[] parameters)
		{
		}

		public void FatalError(string format, params object[] parameters)
		{
		}

		#endregion
	}
}