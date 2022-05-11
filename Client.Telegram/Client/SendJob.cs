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
	private readonly IMongoCollection<Link> _linkCollection;
	private readonly IMongoCollection<PostedLink> _postedCollection;

	public SendJob(ISender sender, IMongoDatabase dbClient, ILogger<SendJob> logger)
	{
		_sender = sender;
		_logger = logger;
		_linkCollection = dbClient.GetCollection<Link>("Links");
		_postedCollection = dbClient.GetCollection<PostedLink>("PostedLinks");
	}

	public async Task Execute(IJobExecutionContext context)
	{
		//TODO get id and username from db
		var channelId = 000000000;
		var channelUserName = "";

		var limit = 1;
		var categories = new[] {"General Categories"};
		var exceptCategories = new[] {"Gay"};
		var documents = GetLinks(limit, categories, exceptCategories);
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

	private List<Link> GetLinks(int limit, IEnumerable<string> categories, string[] exceptCategories)
	{
		foreach (var category in categories.Shuffle())
		{
			var filter = Builders<LinkWithPosted>.Filter.And(
			Builders<LinkWithPosted>.Filter.AnyIn(link => link.Category, new List<string>{category}),
				Builders<LinkWithPosted>.Filter.AnyNin(link => link.Category, exceptCategories),
				Builders<LinkWithPosted>.Filter.Exists(link => link.Urls),
				Builders<LinkWithPosted>.Filter.Ne(link => link.Urls, Array.Empty<string>()),
				Builders<LinkWithPosted>.Filter.Or(
					Builders<LinkWithPosted>.Filter.Eq(link => link.ErrorMessage, string.Empty),
					Builders<LinkWithPosted>.Filter.Exists(link => link.ErrorMessage, false)));

			var documents = _linkCollection
				.Aggregate()
				.Lookup<Link, PostedLink, LinkWithPosted>(_postedCollection, fromType => fromType.SourceUrl,
					targetType => targetType.SourceUrl, output => output.PostedLinks)
				.Match(link => !link.PostedLinks.Any())
				.Match(filter)
				.SortByDescending(link => link.UpVotes)
				.ToEnumerable()
				.Take(limit)
				.Cast<Link>()
				.ToList();
			if (!documents.Any())
				_logger.LogWarning($"Documents count is 0 for {category}");
			else
				return documents;
		}
		_logger.LogWarning("All categories is empty");
		return new List<Link>();
	}
}