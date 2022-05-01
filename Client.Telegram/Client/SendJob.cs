using Client.Telegram.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
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

	public SendJob(WTelegram.Client client, IMongoClient dbClient, ILogger<SendJob> logger)
	{
		_logger = logger;
		var redditDb = dbClient.GetDatabase("Reddit");
		_linkCollection = redditDb.GetCollection<Link>("Links");
		var senderDb = dbClient.GetDatabase("Sender");
		_postedCollection = senderDb.GetCollection<PostedLink>("PostedLinks");
		_client = client;
	}

	public async Task Execute(IJobExecutionContext context)
	{
		await _client.LoginUserIfNeeded();
		var chats = await _client.Messages_GetAllChats();
		var channel = chats.chats.FirstOrDefault(chat => chat.Value is Channel {username: "test_channel"}).Value as Channel;
		Func<Link, bool> filter = link =>
		{
			var filter = Builders<PostedLink>.Filter.Eq(field => field.SourceUrl, link.SourceUrl);
			return !_postedCollection.Find(filter).Any() && link.UpVotes > 1000;
		};
		var preFilter = Builders<Link>.Filter.And(
			Builders<Link>.Filter.Exists(link => link.Urls),
			Builders<Link>.Filter.Ne(link => link.Urls, Array.Empty<string>()),
			Builders<Link>.Filter.Or(
			Builders<Link>.Filter.Eq(link => link.ErrorMessage, String.Empty),
				Builders<Link>.Filter.Exists(link => link.ErrorMessage, false)));

		var limit = 1;
		var documents = _linkCollection.Find(preFilter).SortByDescending(field => field.UpVotes).ToEnumerable()
			.Where(filter).Take(limit).ToList();
		if (documents.Count() == 0)
			_logger.LogWarning("Documents count is 0");
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
		_client.Dispose();
	}
}