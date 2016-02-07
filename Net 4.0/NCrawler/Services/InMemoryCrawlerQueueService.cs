using System.Collections.Generic;

using NCrawler.Utils;

namespace NCrawler.Services
{
	public class InMemoryCrawlerQueueService : CrawlerQueueServiceBase
	{
		#region Readonly & Static Fields

		private readonly Stack<CrawlerQueueEntry> m_Stack = new Stack<CrawlerQueueEntry>();

		#endregion

		#region Instance Methods

		protected override long GetCount()
		{
			return m_Stack.Count;
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			return m_Stack.Count == 0 ? null : m_Stack.Pop();
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			m_Stack.Push(crawlerQueueEntry);
		}

		#endregion
	}
}