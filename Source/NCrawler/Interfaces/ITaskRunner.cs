using System;
using System.ComponentModel;

namespace NCrawler.Interfaces
{
	public interface ITaskRunner
	{
		/// <summary>
		/// Returns true on success run without timeout
		/// </summary>
		/// <param name="action"></param>
		/// <param name="maxRuntime"></param>
		/// <returns>True on success</returns>
		bool RunSync(Action<CancelEventArgs> action, TimeSpan maxRuntime);
	}
}