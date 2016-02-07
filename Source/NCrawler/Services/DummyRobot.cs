using System;

using NCrawler.Interfaces;

namespace NCrawler.Services
{
	public class DummyRobot : IRobot
	{
		#region IRobot Members

		public bool IsAllowed(string userAgent, Uri uri)
		{
			return true;
		}

		#endregion
	}
}