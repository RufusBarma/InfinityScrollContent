using Client.Telegram.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TL;

namespace Client.Telegram.Client;

public class ClientStartup
{
	private WTelegram.Client _client;
	private readonly IMongoCollection<Link> _linkCollection;
	private readonly IMongoCollection<PostedLink> _postedCollection;

	public ClientStartup(IConfiguration configuration, IMongoClient dbClient, ILogger<ClientStartup> logger)
	{
		_client = new WTelegram.Client(what =>
		{
			if (what == "verification_code")
			{
				Console.Write("Code: ");
				return Console.ReadLine();;
			}
			return configuration.GetValue<string>(what); 
		});
		_client.LoginUserIfNeeded();
		var redditDb = dbClient.GetDatabase("Reddit");
		_linkCollection = redditDb.GetCollection<Link>("Links");
		var senderDb = dbClient.GetDatabase("Sender");
		_postedCollection = senderDb.GetCollection<PostedLink>("PostedLinks");
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		ClearCache();
		var chats = await _client.Messages_GetAllChats();
		var channel = chats.chats[0000000000];
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

		var documents = _linkCollection.Find(preFilter).SortByDescending(field => field.UpVotes).ToEnumerable()
			.Where(filter);
		var count = documents.Count();
		var rnd = new Random();
		var limit = 5;
		while (!cancellationToken.IsCancellationRequested && limit-- > 0 && count > 0)
		{
			var document = documents.Skip(rnd.Next(count)).First();
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
				await Task.Delay(1000); //TODO realize messages counting
			}

			var postedLink = new PostedLink
			{
				ChannelId = channel.ID,
				PostDate = DateTime.UtcNow,
				SourceUrl = document.SourceUrl,
			};
			await _postedCollection.InsertOneAsync(postedLink);
		}
	}

	private void ClearCache()
	{
		if (Directory.Exists("tmp"))
			Directory.Delete("tmp", true);
	}
}