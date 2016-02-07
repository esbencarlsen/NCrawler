using System.Collections.Generic;

using NCrawler.Utils;

namespace NCrawler.Services
{
	public class InMemoryCrawlerQueueService : CrawlerQueueServiceBase
	{
		#region Readonly & Static Fields

		private readonly Stack<CrawlerQueueEntry> _stack = new Stack<CrawlerQueueEntry>();

		#endregion

		#region Instance Methods

		protected override long GetCount()
		{
			return _stack.Count;
		}

		protected override CrawlerQueueEntry PopImpl()
		{
			return _stack.Count == 0 ? null : _stack.Pop();
		}

		protected override void PushImpl(CrawlerQueueEntry crawlerQueueEntry)
		{
			_stack.Push(crawlerQueueEntry);
		}

		#endregion
	}
}