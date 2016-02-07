namespace NCrawler.Interfaces
{
	public interface ICrawlerQueue
	{
		#region Instance Properties

		long Count { get; }

		#endregion

		#region Instance Methods

		/// <summary>
		/// 	Get next entry to crawl
		/// </summary>
		/// <returns></returns>
		CrawlerQueueEntry Pop();

		/// <summary>
		/// 	Queue entry to crawl
		/// </summary>
		void Push(CrawlerQueueEntry crawlerQueueEntry);

		#endregion
	}
}