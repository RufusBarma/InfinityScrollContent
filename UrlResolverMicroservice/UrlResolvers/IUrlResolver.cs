using LanguageExt;

namespace UrlResolverMicroservice.UrlResolvers;

public interface IUrlResolver
{
	Task<Either<string, string[]>> ResolveAsync(string url);
	bool CanResolve(string url);
}