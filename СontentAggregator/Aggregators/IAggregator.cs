namespace СontentAggregator.Aggregators;

public interface IAggregator
{
	Task Start(CancellationToken cancellationToken);
}