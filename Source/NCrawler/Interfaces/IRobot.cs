using System;

namespace NCrawler.Interfaces
{
	public interface IRobot
	{
		#region Instance Methods

		bool IsAllowed(string userAgent, Uri uri);

		#endregion
	}
}