using System;
using System.Threading.Tasks;

namespace NCrawler.Pipeline
{
	public class LambdaFilterPipelineStep : IPipelineStep
	{
		private readonly Func<ICrawler, PropertyBag, bool> _predicate;
		private readonly Func<ICrawler, PropertyBag, Task<bool>> _predicate2;

		public LambdaFilterPipelineStep(Func<ICrawler, PropertyBag, bool> predicate, int maxDegreeOfParallelism = 1)
		{
			_predicate = predicate;
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public LambdaFilterPipelineStep(Func<ICrawler, PropertyBag, Task<bool>> predicate, int maxDegreeOfParallelism = 1)
		{
			_predicate2 = predicate;
			MaxDegreeOfParallelism = maxDegreeOfParallelism;
		}

		public Task<bool> Process(ICrawler crawler, PropertyBag propertyBag)
		{
			if (_predicate != null)
			{
				return Task.FromResult(_predicate(crawler, propertyBag));
			}

			if (_predicate2 != null)
			{
				return _predicate2(crawler, propertyBag);
			}

			return Task.FromResult(true);
		}

		public int MaxDegreeOfParallelism { get; }
	}
}