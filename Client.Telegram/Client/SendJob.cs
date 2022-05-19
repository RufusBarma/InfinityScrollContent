using Client.Telegram.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MoreLinq;
using Quartz;

namespace Client.Telegram.Client;

[DisallowConcurrentExecution]
public class SendJob: IJob
{
	private readonly ISender _sender;
	private readonly ILogger<SendJob> _logger;
	private readonly SenderSettings.SenderSettings _settings;
	private readonly IMongoCollection<Link> _linkCollection;
	private readonly IMongoCollection<PostedLink> _postedCollection;

	public SendJob(ISender sender, IMongoDatabase dbClient, ILogger<SendJob> logger, SenderSettings.SenderSettings settings)
	{
		_sender = sender;
		_logger = logger;
		_settings = settings;
		_linkCollection = dbClient.GetCollection<Link>("Links");
		_postedCollection = dbClient.GetCollection<PostedLink>("PostedLinks");
	}

	public async Task Execute(IJobExecutionContext context)
	{
		//TODO get id and username from db
		var channelId = _settings.ChannelId;
		var channelUserName = _settings.ChannelUsername;

		var limit = 1;
		var categories = _settings.Categories;
		var exceptCategories = _settings.ExceptCategories;
		var documents = await (await GetLinks(categories, exceptCategories)).Where(link => link.Urls.Length <= 10).Take(limit).ToListAsync();
		await foreach (var link in _sender.Send(documents, channelId, channelUserName, context.CancellationToken))
		{
			var postedLink = new PostedLink
			{
				ChannelId = channelId,
				ChannelUserName = channelUserName,
				PostDate = DateTime.UtcNow,
				SourceUrl = link.SourceUrl,
			};
			await _postedCollection.InsertOneAsync(postedLink);
		}
	}

	private IAsyncEnumerable<Link> GetLinks(string category, string[] exceptCategories)
	{
		var filters = new List<FilterDefinition<LinkWithPosted>>
		{
			Builders<LinkWithPosted>.Filter.AnyNin(link => link.Category, exceptCategories),
			Builders<LinkWithPosted>.Filter.Exists(link => link.Urls),
			Builders<LinkWithPosted>.Filter.Ne(link => link.Urls, Array.Empty<string>()),
			Builders<LinkWithPosted>.Filter.Or(
				Builders<LinkWithPosted>.Filter.Eq(link => link.ErrorMessage, string.Empty),
				Builders<LinkWithPosted>.Filter.Exists(link => link.ErrorMessage, false))
		};
		if (!string.IsNullOrEmpty(category))
			filters.Add(Builders<LinkWithPosted>.Filter.AnyIn(link => link.Category, new List<string> {category}));
		var filter = Builders<LinkWithPosted>.Filter.And(filters);

		var documents = _linkCollection
			.Aggregate()
			.Lookup<Link, PostedLink, LinkWithPosted>(_postedCollection, fromType => fromType.SourceUrl,
				targetType => targetType.SourceUrl, output => output.PostedLinks)
			.Match(link => !link.PostedLinks.Any())
			.Match(filter)
			.SortByDescending(link => link.UpVotes)
			.ToEnumerable()
			.Cast<Link>()
			.ToAsyncEnumerable()
			.WhereAwait(async link =>
			{
				var parallelFound = link.Urls.Select(url => IsFound(url)).ToList();
				await Task.WhenAll(parallelFound);
				return parallelFound.All(url => url.Result);
			});
		return documents;
	}

	private async Task<IAsyncEnumerable<Link>> GetLinks(IEnumerable<string> categories, string[] exceptCategories)
	{
		foreach (var category in categories.Shuffle())
		{
			var documents = GetLinks(category, exceptCategories);
			if (!await documents.AnyAsync())
				_logger.LogWarning($"Documents count is 0 for {category}");
			else
				return documents;
		}

		var documentsWithoutCategory = GetLinks(string.Empty, exceptCategories);
		if (!await documentsWithoutCategory.AnyAsync())
			_logger.LogWarning("All categories is empty");
		return documentsWithoutCategory;
	}

	private async Task<bool> IsFound(string url)
	{
		var client = new HttpClient(new HttpClientHandler{AllowAutoRedirect = false});
		var result = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		return result.IsSuccessStatusCode;
	}
}