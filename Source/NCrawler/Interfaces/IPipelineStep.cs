namespace NCrawler.Interfaces
{
	public interface IPipelineStep
	{
		/// <summary>
		/// Return true to continue
		/// </summary>
		/// <param name="propertyBag"></param>
		/// <returns></returns>
		bool Process(PropertyBag propertyBag);

		bool ProcessInParallel { get; }
	}
}