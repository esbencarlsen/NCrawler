using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace NCrawler
{
	public partial class CrawlerConfiguration
	{
		public void Run()
		{
			RunAsync().Wait();
		}

		public Task RunAsync()
		{
			TransformBlock<Uri, PropertyBag> ingestBlock = new TransformBlock<Uri, PropertyBag>(input =>
			{
				PropertyBag result = new PropertyBag
				{
					OriginalUrl = input.ToString(),
					UserAgent = _userAgent,
					Step = new CrawlStep(input, 0)
				};

				return result;
			}, new ExecutionDataflowBlockOptions
			{
				MaxDegreeOfParallelism = MaxDegreeOfParallelism
			});

			TransformBlock<PropertyBag, PropertyBag> ingestBlockForAggregation =
				new TransformBlock<PropertyBag, PropertyBag>(input => input, new ExecutionDataflowBlockOptions
				{
					MaxDegreeOfParallelism = MaxDegreeOfParallelism
				});

			CrawlIngestionHelper crawlIngestionHelper = new CrawlIngestionHelper(ingestBlockForAggregation, _userAgent);
			TransformBlock<PropertyBag, PropertyBag>[] pipeline = Pipeline
				.Select(pipelineStep =>
				{
					return new TransformBlock<PropertyBag, PropertyBag>(async propertyBag =>
					{
						if (propertyBag.StopPipelining)
						{
							return propertyBag;
						}

						try
						{
							propertyBag.StopPipelining = !await pipelineStep.Process(crawlIngestionHelper, propertyBag);
						}
						catch (Exception exception)
						{
							propertyBag.Exceptions.Add(exception);
						}

						return propertyBag;
					}, new ExecutionDataflowBlockOptions
					{
						MaxDegreeOfParallelism = pipelineStep.MaxDegreeOfParallelism
					});
				})
				.ToArray();

			ActionBlock<PropertyBag> terminationCheckerBlock = new ActionBlock<PropertyBag>(propertyBag =>
			{
				if (ingestBlock.InputCount == 0
					&& ingestBlock.OutputCount == 0
					&& !ingestBlock.Completion.IsCompleted
					&& !ingestBlock.Completion.IsCanceled
					&& !ingestBlock.Completion.IsFaulted
					&& ingestBlockForAggregation.InputCount == 0
					&& ingestBlockForAggregation.OutputCount == 0)
				{
					if (pipeline.Any(transformBlock => transformBlock.InputCount != 0 || transformBlock.OutputCount != 0))
					{
						return;
					}

					ingestBlock.Complete();
				}
			}, new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1});

			ingestBlock.LinkTo(ingestBlockForAggregation, new DataflowLinkOptions {PropagateCompletion = true});
			TransformBlock<PropertyBag, PropertyBag> previous = ingestBlockForAggregation;
			foreach (TransformBlock<PropertyBag, PropertyBag> transformBlock in pipeline)
			{
				previous.LinkTo(transformBlock, new DataflowLinkOptions {PropagateCompletion = true});
				previous = transformBlock;
			}

			previous.LinkTo(terminationCheckerBlock, new DataflowLinkOptions {PropagateCompletion = true});
			foreach (Uri startUri in StartUris)
			{
				ingestBlock.Post(startUri);
			}

			return terminationCheckerBlock.Completion;
		}

		private class CrawlIngestionHelper : ICrawler
		{
			private readonly TransformBlock<PropertyBag, PropertyBag> _transformBlock;
			private readonly string _userAgent;

			public CrawlIngestionHelper(TransformBlock<PropertyBag, PropertyBag> transformBlock,
				string userAgent)
			{
				_transformBlock = transformBlock;
				_userAgent = userAgent;
			}

			public void Crawl(Uri uri, PropertyBag referer)
			{
				int depth = referer?.Step?.Depth + 1 ?? 0;
				_transformBlock.Post(new PropertyBag
				{
					Step = new CrawlStep(uri, depth),
					Referrer = referer?.Referrer,
					UserAgent = _userAgent
				});
			}

			public void Stop()
			{
				_transformBlock.Complete();
			}
		}
	}
}