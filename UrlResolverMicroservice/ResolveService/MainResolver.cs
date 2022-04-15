using LanguageExt;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MoreLinq;
using UrlResolverMicroservice.Models;
using UrlResolverMicroservice.UrlResolvers;

namespace UrlResolverMicroservice.ResolveService;

public class MainResolver : IMainResolver
{
	private readonly IMongoCollection<Link> _linkCollection;
	private readonly Dictionary<string, LinkType> _linkTypes;
	private readonly IEnumerable<IUrlResolver> _urlResolvers;
	private readonly ILogger<MainResolver> _logger;

	public MainResolver(IMongoClient mongoClient, IEnumerable<IUrlResolver> urlResolvers, ILogger<MainResolver> logger)
	{
		_urlResolvers = urlResolvers;
		_logger = logger;
		var redditDb = mongoClient.GetDatabase("Reddit");
		_linkCollection = redditDb.GetCollection<Link>("Links");
		_linkTypes = new Dictionary<string, LinkType>();
		"jpeg,png,bmp,webp,tiff,avif".Split(',').ForEach(extension => _linkTypes.Add(extension, LinkType.Img));
		"mp4,mpeg,ogg,gifv".Split(',').ForEach(extension => _linkTypes.Add(extension, LinkType.Video));
		_linkTypes.Add("gif", LinkType.Gif);
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		var filter = Builders<Link>.Filter.In("Urls", new[] { null, Array.Empty<string>() });
		var emptyUrls = _linkCollection.Find(filter).ToEnumerable();
		foreach (var link in emptyUrls)
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			var urlsEither = await GetUrlsAsync(link.SourceUrl);
			var skipFlag = false;
			var update = urlsEither.Match(urls => Builders<Link>.Update
					.Set(updLink => updLink.Urls, urls)
					.Set(updLink => updLink.Type, GetLinkType(urls.First()))
					.Set(updLink => updLink.IsGallery, urls.Length > 1),
				error =>
				{
					if (error == "Skip")
						skipFlag = true;
					return Builders<Link>.Update.Set(updLink => updLink.ErrorMessage, error);
				});
			if (skipFlag)
				continue;

			var updateFilter = Builders<Link>.Filter.Eq("_id", link._id);
			await _linkCollection.UpdateOneAsync(updateFilter, update, new UpdateOptions(){IsUpsert = true});
		}
		_logger.LogInformation("Resolve completed");
	}

	private async Task<Either<string, string[]>> GetUrlsAsync(string url)
	{
		if (Path.HasExtension(url))
			if (url.Contains("imgur"))
				url = Path.ChangeExtension(url, null);
			else
				return new[] { url };
		return await Prelude.Optional(_urlResolvers.FirstOrDefault(resolver => resolver.CanResolve(url)))
			.MatchAsync(
				async resolver =>
				{
					if (resolver.LimitRich())
						return "Skip";
					return await resolver.ResolveAsync(url);
				}, 
				() => {
					_logger.LogWarning("Resolver doesn't exist for {Url}", url);
					return "Resolver doesn't exist";
				});
	}

	private LinkType GetLinkType(string url)
	{
		if (!Path.HasExtension(url))
			return LinkType.None;
		_linkTypes.TryGetValue(Path.GetExtension(url), out var defaultType);
		return defaultType;
	}
}