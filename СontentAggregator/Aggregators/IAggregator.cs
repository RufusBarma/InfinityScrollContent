namespace СontentAggregator.Aggregators;

public interface IAggregator
{
	Task Start();
	void Stop();
}