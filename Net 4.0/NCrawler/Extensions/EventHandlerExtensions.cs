using System;

namespace NCrawler.Extensions
{
	public static class EventHandlerExtensions
	{
		/// <summary>
		/// Wrapper for executing an event
		/// </summary>
		public static void ExecuteEvent<T>(this EventHandler<T> handler, object sender, Func<T> args) where T : EventArgs
		{
			if (!handler.IsNull())
			{
				handler(sender, args());
			}
		}
	}
}