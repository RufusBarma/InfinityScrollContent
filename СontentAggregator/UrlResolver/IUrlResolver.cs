namespace СontentAggregator.UrlResolver;

public interface IUrlResolver
{
	Task<IEnumerable<string>> ResolveAsync(string url);
}