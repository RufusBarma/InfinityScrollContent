using System.Threading.Tasks.Dataflow;
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

	public MainResolver(IMongoDatabase database, IEnumerable<IUrlResolver> urlResolvers, ILogger<MainResolver> logger)
	{
		_urlResolvers = urlResolvers;
		_logger = logger;
		_linkCollection = database.GetCollection<Link>("Links");
		_linkTypes = new Dictionary<string, LinkType>();
		"jpeg,png,bmp,webp,tiff,avif".Split(',').ForEach(extension => _linkTypes.Add(extension, LinkType.Img));
		"mp4,mpeg,ogg,gifv".Split(',').ForEach(extension => _linkTypes.Add(extension, LinkType.Video));
		_linkTypes.Add("gif", LinkType.Gif);
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		var updateLinksInDbBlock =
			new ActionBlock<IEnumerable<IEnumerable<(Link link, Either<string, string[]> urlsEither)>>>(UpdateLinksInDbBatch);
		var updateLinksInDbBatchBlock =
			new BatchBlock<IEnumerable<(Link link, Either<string, string[]> urlsEither)>>(10);
		updateLinksInDbBatchBlock.LinkTo(updateLinksInDbBlock);

		var resolverTransformBlocks = _urlResolvers.ToDictionary(urlResolver => urlResolver, _ =>
		{
			var batchBlock = new BatchBlock<Link>(2);
			var resolveBlock = new TransformBlock<IEnumerable<Link>, IEnumerable<(Link link, Either<string, string[]>)>>(ResolveUrl, new ExecutionDataflowBlockOptions()
			{
				MaxDegreeOfParallelism = 1,
				CancellationToken = cancellationToken
			});
			batchBlock.LinkTo(resolveBlock);
			resolveBlock.LinkTo(updateLinksInDbBatchBlock);
			return batchBlock;
		});
		foreach (var link in _linkCollection.GetEmptyUrls())
		{
			if (cancellationToken.IsCancellationRequested)
				break;
			var url = link.SourceUrl;
			if (Path.HasExtension(url))
				if (url.Contains("imgur"))
					url = Path.ChangeExtension(url, null);
				else
				{
					Either<string, string[]> urls = new[] {url};
					await updateLinksInDbBatchBlock.SendAsync(new []{(link, urls)});
					continue;
				}
			var resolver = _urlResolvers.FirstOrDefault(resolver => resolver.CanResolve(url));
			if (resolver == null)
			{
				_logger.LogWarning("Resolver doesn't exist for {Url}", url);
				Either<string, string[]> error = "Resolver doesn't exist";
				await updateLinksInDbBatchBlock.SendAsync(new []{(link, error)});
				continue;
			}

			await resolverTransformBlocks[resolver].SendAsync(link);
		}
		updateLinksInDbBatchBlock.Complete();
		await updateLinksInDbBatchBlock.Completion;

		_logger.LogInformation("Resolve completed");
	}

	private async Task<IEnumerable<(Link link, Either<string, string[]>)>> ResolveUrl(IEnumerable<Link> links)
	{
		return await Task.WhenAll(links.Select(async link => (link, await GetUrlsAsync(link.SourceUrl))));
	}

	private async Task UpdateLinksInDbBatch(IEnumerable<IEnumerable<(Link link, Either<string, string[]> urlsEither)>> resolvedLinksBatch)
	{
		await UpdateLinksInDb(resolvedLinksBatch.SelectMany(resolvedLinks => resolvedLinks));
	}

	private async Task UpdateLinksInDb(IEnumerable<(Link link, Either<string, string[]> urlsEither)> resolvedLinks)
	{
		var g = resolvedLinks.ToList();
		_logger.LogInformation("Update for {resolverLinkdsCount}", g.Count);
		foreach (var (link, urlsEither) in resolvedLinks)
		{
			var skipFlag = false;
			var update = urlsEither.Match(urls => Builders<Link>.Update
					.Set(updLink => updLink.Urls, urls)
					.Set(updLink => updLink.Type, GetLinkType(urls.FirstOrDefault()))
					.Set(updLink => updLink.IsGallery, urls.Length > 1),
				error =>
				{
					if (error == "Skip")
						skipFlag = true;
					return Builders<Link>.Update.Set(updLink => updLink.ErrorMessage, error);
				});
			if (skipFlag)
			{
				_logger.LogInformation("Skip {link.SourceUrl}", link.SourceUrl);
				continue;
			}
			_logger.LogInformation("Resolved {link.SourceUrl}", link.SourceUrl);
			var updateFilter = Builders<Link>.Filter.Eq("_id", link._id);
			await _linkCollection.UpdateOneAsync(updateFilter, update, new UpdateOptions(){IsUpsert = true});
		}
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

	private LinkType GetLinkType(string? url)
	{
		if (url == null || !Path.HasExtension(url))
			return LinkType.None;
		var photoExts = new[] {".png", ".jpeg", ".jpg"};
		var extension = Path.GetExtension(url).ToLower();
		if (photoExts.Any(ext => ext == extension))
			return LinkType.Img;
		if (extension == ".gif")
			return LinkType.Gif;
		if (extension == ".mp4")
			return LinkType.Video;
		return LinkType.None;
	}
}