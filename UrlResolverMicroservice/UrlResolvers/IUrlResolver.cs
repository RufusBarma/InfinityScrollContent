namespace UrlResolverMicroservice.UrlResolvers;

public interface IUrlResolver
{
	Task<IEnumerable<string>> ResolveAsync(string url);
	bool CanResolve(string url);
}