namespace UrlResolverMicroservice.ResolveTask;

public interface IMainResolver
{
	Task Start(CancellationToken cancellationToken);
}