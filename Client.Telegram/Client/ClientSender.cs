using System.Runtime.CompilerServices;
using Client.Telegram.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TL;

namespace Client.Telegram.Client;

public class ClientSender : ISender, IDisposable
{
	private readonly ILogger<ClientSender> _logger;
	private readonly WTelegram.Client _client;
	private readonly IMongoCollection<SavedState> _accessHashCollection;
	private readonly ClientSenderExtensions _extensions;
	private static readonly string[] PhotoExtensions = {"png", "jpeg", "jpg"};

	public ClientSender(ILogger<ClientSender> logger, WTelegram.Client client, IMongoCollection<SavedState> accessHashCollection, ClientSenderExtensions extensions)
	{
		_logger = logger;
		_client = client;
		_accessHashCollection = accessHashCollection;
		_extensions = extensions;
	}

	public async IAsyncEnumerable<Link> Send(IEnumerable<Link> links, long channelId, string channelUserName, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var me = await _client.LoginUserIfNeeded();
		await _client.LoadAccessHash(me, _accessHashCollection);
		var channel = await _client.GetChannel(channelId, channelUserName);

		foreach (var link in links)
		{
			if (cancellationToken.IsCancellationRequested)
				break;

			_logger.LogInformation("Sending link with source: {Source}", link.SourceUrl);

			var mediaGroups = link.Urls.Select(url => (InputMedia)
					(PhotoExtensions.Any(ext => ext == Path.GetExtension(url).Remove(0, 1).ToLower())
						? new InputMediaPhotoExternal {url = url}
						: new InputMediaDocumentExternal {url = url}))
				.GroupBy(media => media.GetType());
			foreach (var mediaGroup in mediaGroups)
			foreach (var urlChunk in mediaGroup.Chunk(10))
			{
				var description = string.IsNullOrEmpty(link.Description)? "": Markdown.Escape(link.Description) + "\n\n";
				var sourceLink = string.IsNullOrEmpty(link.PermaLink)? "": $"[Source]({link.PermaLink})";
				var tags = string.Join(' ', link.Category.Select(category => '#' + 
				                                                             category
					                                                             .Replace(' ', '_').Replace('-', '_')
					                                                             .Replace('/', '_').Replace('\\', '_')));
				var caption = (description + "*Categories:* " + Markdown.Escape(tags) + "\n\n" + "*" + sourceLink + "*").Trim();
				var entities = _client.MarkdownToEntities(ref caption);
				await _extensions.SafeSendAlbumAsync(_client, channel, urlChunk, caption, entities: entities);
				await Task.Delay(urlChunk.Length * 2500); // 24 urls per minute 2500 * 30 = 60000 ms 
				//TODO realize messages counting
				_logger.LogInformation("Successful sent link with source: {Source}", link.SourceUrl);
				yield return link;
			}
		}

		await _client.SaveAccessHash(me, _accessHashCollection);
	}

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			// _client.Dispose(); //TODO throw exception in LoginIfNeeded (solve please)
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~ClientSender()
	{
		Dispose(false);
	}
}