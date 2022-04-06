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
		// var chats = await _client.Contacts_ResolveUsername("");
		// await _client.SendMessageAsync(chats, "Hello, World");
	}
}