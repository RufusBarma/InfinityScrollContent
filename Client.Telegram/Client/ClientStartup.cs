using Microsoft.Extensions.Configuration;
using TL;

namespace Client.Telegram.Client;

public class ClientStartup
{
	private WTelegram.Client _client;

	public ClientStartup(IConfiguration configuration)
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
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		var chats = await _client.Messages_GetAllChats();
		var channel = chats.chats[0000000000];

		// var file1 = await _client.UploadFileAsync(@"1.jpg", withCompression:true);
		// var file2 = await _client.UploadFileAsync(@"2.jpg", withCompression:true);
		// var file3 = await _client.UploadFileAsync(@"3.jpg", withCompression:true);
		// var uploaded = new[]
		// {
		// 	new InputMediaUploadedPhoto {file = file1},
		// 	new InputMediaUploadedPhoto {file = file2},
		// 	new InputMediaUploadedPhoto {file = file3}
		// };
		var medias = new[]
			{
				"url1",
				"url2",
			}
			.Select(url => new InputMediaPhotoExternal {url = url})
			.ToArray();
		await _client.SafeSendAlbumAsync(channel, medias);
	}
}