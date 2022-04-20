using System.Linq.Expressions;
using Bot.Telegram.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TL;

namespace Client.Telegram.Client;

public class ClientStartup
{
	private WTelegram.Client _client;
	private readonly IMongoCollection<Link> _linkCollection;

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
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		var chats = await _client.Messages_GetAllChats();
		var channel = chats.chats[0000000000];
		Expression<Func<Link, bool>> filter = link =>
			link.UpVotes > 500 && string.IsNullOrEmpty(link.ErrorMessage) && link.IsGallery;
		var count = (int) _linkCollection.CountDocuments(filter);
		var rnd = new Random();
		var limit = 1;
		while (!cancellationToken.IsCancellationRequested && limit-- > 0)
		{
			var document = await _linkCollection.Find(filter).ToListAsync();//.Limit(1).Skip(rnd.Next(count)).ToListAsync();
			var doc = document[2];
			var photoExts = new[] {"png", "jpeg", "jpg"};
			var urlChunks = doc.Urls.Select(url => (InputMedia)
					(photoExts.Any(ext => ext == Path.GetExtension(url).Remove(0, 1).ToLower())
						? new InputMediaPhotoExternal {url = url}
						: new InputMediaDocumentExternal {url = url}))
				.GroupBy(media => media.GetType());
			foreach (var mediaGroup in urlChunks.Skip(1))
			foreach (var urlChunk in mediaGroup.Chunk(10))
			{
				await _client.SafeSendAlbumAsync(channel, urlChunk);
				await Task.Delay(1000);
			}
		}
	}
}