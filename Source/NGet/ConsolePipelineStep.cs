using System;

using NCrawler;
using NCrawler.Interfaces;

namespace NGet
{
	public class ConsolePipelineStep : IPipelineStep
	{
		#region IPipelineStep Members

		public void Process(Crawler crawler, PropertyBag propertyBag)
		{
			Console.Out.WriteLine(propertyBag.Step.Uri);
		}

		#endregion
	}
}