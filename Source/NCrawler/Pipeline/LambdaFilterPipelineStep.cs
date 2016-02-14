using System;

using NCrawler.Interfaces;

namespace NCrawler.Pipeline
{
	public class LambdaFilterPipelineStep : IPipelineStep
	{
		private readonly Predicate<PropertyBag> _predicate;

		public LambdaFilterPipelineStep(Predicate<PropertyBag> predicate, bool runInParallel = false)
		{
			_predicate = predicate;
			ProcessInParallel = runInParallel;
		}

		public bool Process(PropertyBag propertyBag)
		{
			return _predicate(propertyBag);
		}

		public bool ProcessInParallel { get; }
	}
}