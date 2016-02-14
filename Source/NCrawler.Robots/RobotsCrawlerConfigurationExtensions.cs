namespace NCrawler.Robots
{
	public static class RobotsCrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration Robots(this CrawlerConfiguration crawlerConfiguration)
		{
			crawlerConfiguration.AddPipelineStep(new RobotsPipelineStep(crawlerConfiguration.Logger));
			return crawlerConfiguration;
		}
	}
}