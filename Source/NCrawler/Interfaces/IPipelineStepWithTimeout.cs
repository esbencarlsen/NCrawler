using System;

namespace NCrawler.Interfaces
{
	public interface IPipelineStepWithTimeout : IPipelineStep
	{
		TimeSpan ProcessorTimeout { get; }
	}
}