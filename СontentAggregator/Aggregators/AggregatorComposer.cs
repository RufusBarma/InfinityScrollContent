namespace СontentAggregator.Aggregators;

public class AggregatorComposer
{
	private List<IAggregator> _aggregators;

	public AggregatorComposer(IEnumerable<IAggregator> aggregators) =>
		_aggregators = aggregators.ToList();

	public void Start()
	{
		_aggregators.ForEach(aggregator => aggregator.Start());
	}

	public void Stop()
	{
		_aggregators.ForEach(aggregator => aggregator.Stop());
	}
}