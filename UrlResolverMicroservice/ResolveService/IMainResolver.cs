namespace UrlResolverMicroservice.ResolveService;

public interface IMainResolver
{
	Task Start(CancellationToken cancellationToken);
}