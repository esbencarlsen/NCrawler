namespace NCrawler.Interfaces
{
	public interface IPipelineStep
	{
		void Process(Crawler crawler, PropertyBag propertyBag);
	}
}