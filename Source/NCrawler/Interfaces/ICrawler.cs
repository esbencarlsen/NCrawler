using System;

namespace NCrawler.Interfaces
{
	public interface ICrawler
	{
		void Crawl(Uri uri, PropertyBag referer);
	}
}