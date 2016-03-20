namespace NCrawler.Robots
{
	public static class RobotsCrawlerConfigurationExtensions
	{
		public static CrawlerConfiguration Robots(this CrawlerConfiguration crawlerConfiguration, string searchPath = null)
		{
			crawlerConfiguration.AddPipelineStep(new RobotsPipelineStep(searchPath, crawlerConfiguration.Logger));
			return crawlerConfiguration;
		}
	}
}