namespace СontentAggregator.Aggregators;

public class AggregatorComposer
{
	private List<IAggregator> _aggregators;

	public AggregatorComposer(IEnumerable<IAggregator> aggregators) =>
		_aggregators = aggregators.ToList();

	public async Task Start(CancellationToken cancellationToken)
	{
		var aggregatorTasks = _aggregators.Select(aggregator => aggregator.Start(cancellationToken)).ToArray();
		await Task.WhenAll(aggregatorTasks);
	}
}