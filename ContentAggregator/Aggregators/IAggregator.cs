namespace ContentAggregator.Aggregators;

public interface IAggregator
{
	Task Start(CancellationToken cancellationToken);
}