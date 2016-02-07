namespace NCrawler.Interfaces
{
	public interface ILog
	{
		void Verbose(string format, params object[] parameters);
		void Warning(string format, params object[] parameters);
		void Debug(string format, params object[] parameters);
		void Error(string format, params object[] parameters);
		void FatalError(string format, params object[] parameters);
	}
}