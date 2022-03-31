namespace СontentAggregator.Aggregators;

public class AggregatorComposer
{
	private List<IAggregator> _aggregators;

	public AggregatorComposer(IEnumerable<IAggregator> aggregators) =>
		_aggregators = aggregators.ToList();

	public void Start()
	{
		var aggregatorTasks = _aggregators.Select(aggregator => aggregator.Start()).ToArray();
		Task.WaitAll(aggregatorTasks);
	}

	public void Stop()
	{
		_aggregators.ForEach(aggregator => aggregator.Stop());
	}
}