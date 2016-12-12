using System;

using Nancy.Hosting.Self;

namespace NCrawler.WebServer
{
	public class SimpleWebServer : IDisposable
	{
		private readonly NancyHost _host;

		public SimpleWebServer()
		{
			Port = PortUtils.FindAvailablePort();
			BaseUrl = $"http://localhost:{Port}";
			_host = new NancyHost(new Uri(BaseUrl));
			_host.Start();
		}

		public int Port { get; }
		public string BaseUrl { get; set; }

		public void Dispose()
		{
			_host.Dispose();
		}
	}
}