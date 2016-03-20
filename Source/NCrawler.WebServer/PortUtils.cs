using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NCrawler.WebServer
{
	public static class PortUtils
	{
		public static int FindAvailablePort()
		{
			using (Mutex mutex = new Mutex(false, "PortUtils.FindAvailablePort"))
			{
				try
				{
					mutex.WaitOne();
					IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 0);
					using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
					{
						socket.Bind(endPoint);
						IPEndPoint local = (IPEndPoint)socket.LocalEndPoint;
						return local.Port;
					}
				}
				finally
				{
					mutex.ReleaseMutex();
				}
			}
		}
	}
}