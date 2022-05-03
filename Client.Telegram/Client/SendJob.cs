using Client.Telegram.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MoreLinq;
using Quartz;
using TL;

namespace Client.Telegram.Client;

[DisallowConcurrentExecution]
public class SendJob: IJob
{
	private readonly ILogger<SendJob> _logger;
	private WTelegram.Client _client;
	private readonly IMongoCollection<Link> _linkCollection;
	private readonly IMongoCollection<PostedLink> _postedCollection;
	private readonly IMongoCollection<SavedState> _accessHashCollection;

	public SendJob(WTelegram.Client client, IMongoClient dbClient, ILogger<SendJob> logger)
	{
		_logger = logger;
		var redditDb = dbClient.GetDatabase("b7kltbu2j3tfrgn");
		_linkCollection = redditDb.GetCollection<Link>("Links");
		var senderDb = dbClient.GetDatabase("b7kltbu2j3tfrgn");
		_postedCollection = senderDb.GetCollection<PostedLink>("PostedLinks");
		var accessHashDb = dbClient.GetDatabase("b7kltbu2j3tfrgn");
		_accessHashCollection = accessHashDb.GetCollection<SavedState>("AccessHash");
		_client = client;
	}

	public async Task Execute(IJobExecutionContext context)
	{
		var me = await _client.LoginUserIfNeeded();
		await _client.LoadAccessHash(me, _accessHashCollection);
		//TODO get id and username from db
		var channelId = 000000000;
		var channelUserName = "";

		var channel = await _client.GetChannel(channelId, channelUserName);

		var limit = 1;
		var documents = GetLinks(limit);
		while (!context.CancellationToken.IsCancellationRequested && limit-- > 0 && documents.Count > 0)
		{
			var document = documents.First();
			_logger.LogInformation("Sending link with source: {Source}", document.SourceUrl);
			var photoExts = new[] {"png", "jpeg", "jpg"};
			var mediaGroups = document.Urls.Select(url => (InputMedia)
					(photoExts.Any(ext => ext == Path.GetExtension(url).Remove(0, 1).ToLower())
						? new InputMediaPhotoExternal {url = url}
						: new InputMediaDocumentExternal {url = url}))
				.GroupBy(media => media.GetType());
			foreach (var mediaGroup in mediaGroups)
			foreach (var urlChunk in mediaGroup.Chunk(10))
			{
				var sourceLink = string.IsNullOrEmpty(document.PermaLink)? "": $"[Source]({document.PermaLink})";
				var tags = string.Join(' ', document.Category.Select(category => '#' + 
					category
						.Replace(' ', '_').Replace('-', '_')
						.Replace('/', '_').Replace('\\', '_')));
				var caption = ("*Categories:* " + Markdown.Escape(tags) + "\n\n" + "*" + sourceLink + "*").Trim();
				var entities = _client.MarkdownToEntities(ref caption);
				await _client.SafeSendAlbumAsync(channel, urlChunk, caption, entities: entities);
				documents.Remove(document);
				await Task.Delay(urlChunk.Length * 2500); // 24 urls per minute 2500 * 30 = 60000 ms 
				//TODO realize messages counting
			}
			_logger.LogInformation("Successful sent link with source: {Source}", document.SourceUrl);

			var postedLink = new PostedLink
			{
				ChannelId = channel.ID,
				ChannelUserName = channel.username,
				PostDate = DateTime.UtcNow,
				SourceUrl = document.SourceUrl,
			};
			await _postedCollection.InsertOneAsync(postedLink);
		}

		await _client.SaveAccessHash(me, _accessHashCollection);
		_client.Dispose();
	}

	private List<Link> GetLinks(int limit)
	{
		Func<Link, bool> filter = link =>
		{
			var filter = Builders<PostedLink>.Filter.Eq(field => field.SourceUrl, link.SourceUrl);
			return !_postedCollection.Find(filter).Any();
		};

		var categories = new List<string> {"General Categories"}.Shuffle();
		foreach (var category in categories)
		{
			var exceptCategories = new[] {"ignore"};
			var preFilter = Builders<Link>.Filter.And(
				// Builders<Link>.Filter.Gt(link => link.UpVotes, 1000),
				Builders<Link>.Filter.AnyIn(link => link.Category, new List<string>{category}),
				Builders<Link>.Filter.AnyNin(link => link.Category, exceptCategories),
				Builders<Link>.Filter.Exists(link => link.Urls),
				Builders<Link>.Filter.Ne(link => link.Urls, Array.Empty<string>()),
				Builders<Link>.Filter.Or(
					Builders<Link>.Filter.Eq(link => link.ErrorMessage, string.Empty),
					Builders<Link>.Filter.Exists(link => link.ErrorMessage, false)));

			var documents = _linkCollection.Find(preFilter).SortByDescending(field => field.UpVotes).ToEnumerable()
				.Where(filter).Take(limit).ToList();
			if (!documents.Any())
				_logger.LogWarning($"Documents count is 0 for {category}");
			else
				return documents;
		}
		_logger.LogWarning("All categories is empty");
		return new List<Link>();
	}
}