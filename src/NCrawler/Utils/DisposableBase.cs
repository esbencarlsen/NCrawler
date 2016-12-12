using System;

namespace NCrawler.Utils
{
	public abstract class DisposableBase : IDisposable
	{
		protected bool Disposed { get; private set; }

		/// <summary>
		/// Do cleanup here
		/// </summary>
		protected abstract void Cleanup();

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || Disposed)
			{
				return;
			}

			Cleanup();
			Disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);

			// Take off the finalization queue to prevent finalization from executing a second time.
			GC.SuppressFinalize(this);
		}
	}
}