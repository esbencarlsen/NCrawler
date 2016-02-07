namespace NCrawler.Interfaces
{
	public interface ICrawlerHistory
	{
		#region Instance Properties

		long RegisteredCount { get; }

		#endregion

		#region Instance Methods

		/// <summary>
		/// 	Register a unique key
		/// </summary>
		/// <param name = "key">key to register</param>
		/// <returns>false if key has already been registered else true</returns>
		bool Register(string key);

		#endregion
	}
}