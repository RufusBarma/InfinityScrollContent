using System.Linq.Expressions;
using Bot.Telegram.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bot.Telegram.Bot;

public class MailingQueue
{
	private readonly ITelegramBotClient _bot;
	private readonly ChatId _chat;
	private IMongoCollection<Channel> _channelCollection;
	private readonly IMongoCollection<Link> _linkCollection;
	private ILogger<MailingQueue> _logger;

	public MailingQueue(IMongoClient dbClient, ITelegramBotClient bot, ILogger<MailingQueue> logger)
	{
		_logger = logger;
		_bot = bot;
		_chat = new ChatId(""); //test channel
		var redditDb = dbClient.GetDatabase("Reddit");
		_linkCollection = redditDb.GetCollection<Link>("Links");
		_channelCollection = redditDb.GetCollection<Channel>("Channels");
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		Expression<Func<Link, bool>> filter = link =>
			link.UpVotes > 500 && string.IsNullOrEmpty(link.ErrorMessage) && link.IsGallery;
		var count = (int) _linkCollection.CountDocuments(filter);
		var rnd = new Random();
		var limit = 5;
		while (!cancellationToken.IsCancellationRequested && limit-- > 0)
		{
			var document = await _linkCollection.Find(filter).Limit(1).Skip(rnd.Next(count)).ToListAsync();
			var doc = document[2];
			var photoExts = new[] {"png", "jpeg", "jpg"};
			var urlChunks = doc.Urls.Select(url => (IAlbumInputMedia)
					(photoExts.Any(ext => ext == Path.GetExtension(url).Remove(0, 1).ToLower())
						? new InputMediaPhoto(url)
						: new InputMediaVideo(url)))
				.GroupBy(media => media.Type);
			foreach (var mediaGroup in urlChunks)
			foreach (var urlChunk in mediaGroup.Chunk(10))
			{
				await _bot.SendMediaGroupAsync(_chat, new List<IAlbumInputMedia>());
				await _bot.SendMediaGroupAsync(_chat, urlChunk);
				await Task.Delay(1000);
			}
		}
	}
}