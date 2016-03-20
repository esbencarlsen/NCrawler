namespace NCrawler.HtmlProcessor.Interfaces
{
	public interface ISubstitution
	{
		string Substitute(string original, CrawlStep crawlStep);
	}
}