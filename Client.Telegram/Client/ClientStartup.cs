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
		ClearCache();
		var chats = await _client.Messages_GetAllChats();
		var channel = chats.chats[0000000000];
		Expression<Func<Link, bool>> filter = link =>
			link.UpVotes > 500 && string.IsNullOrEmpty(link.ErrorMessage) && link.IsGallery;
		var count = (int) _linkCollection.CountDocuments(filter);
		var rnd = new Random();
		var limit = 1;
		while (!cancellationToken.IsCancellationRequested && limit-- > 0)
		{
			var document = await _linkCollection.Find(filter).Skip(rnd.Next(count)).FirstOrDefaultAsync();
			var photoExts = new[] {"png", "jpeg", "jpg"};
			var mediaGroups = document.Urls.Select(url => (InputMedia)
					(photoExts.Any(ext => ext == Path.GetExtension(url).Remove(0, 1).ToLower())
						? new InputMediaPhotoExternal {url = url}
						: new InputMediaDocumentExternal {url = url}))
				.GroupBy(media => media.GetType());
			foreach (var mediaGroup in mediaGroups)
			foreach (var urlChunk in mediaGroup.Chunk(10))
			{
				var tags = string.Join(", ", document.Category.Select(category => '#' + category.Replace(' ', '_')));
				await _client.SafeSendAlbumAsync(channel, urlChunk, tags);
				await Task.Delay(1000);
			}
		}
	}

	private void ClearCache()
	{
		if (Directory.Exists("tmp"))
			Directory.Delete("tmp", true);
	}
}