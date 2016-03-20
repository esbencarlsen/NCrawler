using System;

namespace NCrawler
{
	public interface ICrawler
	{
		void Crawl(Uri uri, PropertyBag referer);
		void Stop();
	}
}