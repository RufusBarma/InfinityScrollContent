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
			var urls = (await GetUrlsAsync(link.SourceUrl)).ToArray();
			if (urls.Any())
			{
				link.Urls = urls.ToArray();
				link.Type = link.Type = GetLinkType(link.Urls.First());
				link.IsGallery = urls.Length > 1;
			}
			else
				link.DeleteMe = true;

			var updateFilter = Builders<Link>.Filter.Eq("_id", link._id);
			var update = Builders<Link>.Update
				.Set(updLink => updLink.Urls, link.Urls)
				.Set(updLink => updLink.Type, link.Type)
				.Set(updLink => updLink.IsGallery, link.IsGallery);
			if (link.DeleteMe)
				update.Set(updLink => updLink.DeleteMe, link.DeleteMe);
			await _linkCollection.UpdateOneAsync(updateFilter, update);
		}
		_logger.LogInformation("Resolve completed");
	}

	private async Task<IEnumerable<string>> GetUrlsAsync(string url)
	{
		if (Path.HasExtension(url))
			return new[] { url };
		foreach (var resolver in _urlResolvers.Where(resolver => resolver.CanResolve(url)))
		{
			var urls = (await resolver.ResolveAsync(url)).ToArray();
			if (urls.Any())
				return urls;
		}

		_logger.LogWarning("Can't resolve {Url}", url);
		return Array.Empty<string>();
	}

	private LinkType GetLinkType(string url)
	{
		if (!Path.HasExtension(url))
			return LinkType.None;
		_linkTypes.TryGetValue(Path.GetExtension(url), out var defaultType);
		return defaultType;
	}
}