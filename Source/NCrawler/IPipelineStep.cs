using System.Threading.Tasks;

namespace NCrawler
{
	public interface IPipelineStep
	{
		/// <summary>
		/// Return true to continue
		/// </summary>
		/// <param name="crawler"></param>
		/// <param name="propertyBag"></param>
		/// <returns></returns>
		Task<bool> Process(ICrawler crawler, PropertyBag propertyBag);

		int MaxDegreeOfParallelism { get; }
	}
}