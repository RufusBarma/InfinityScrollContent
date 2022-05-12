using MongoDB.Driver;
using TL;

namespace Client.Telegram.Client;

public static class TelegramClientExtensions
{
	public static async Task LoadAccessHash(this WTelegram.Client client, User me, IMongoCollection<SavedState> mongoCollection)
	{
		using var accessHash = await mongoCollection.FindAsync(state => state.Username == me.username);
		var savedState = await accessHash.FirstOrDefaultAsync();
		savedState?.SetAccessHash(client);
	}

	public static async Task SaveAccessHash(this WTelegram.Client client, User me, IMongoCollection<SavedState> mongoCollection)
	{
		var savedState = new SavedState
		{
			Username = me.username,
			Channels = client.AllAccessHashesFor<Channel>().ToList(),
			Users = client.AllAccessHashesFor<User>().ToList()
		};
		await mongoCollection.ReplaceOneAsync(state => state.Username == me.username, savedState, new ReplaceOptions {IsUpsert = true});
	}

	public static async Task<Channel?> GetChannel(this WTelegram.Client client, long channelId, string channelUserName)
	{
		var channelAccessHash = client.GetAccessHashFor<Channel>(channelId);
		if (channelAccessHash == 0)
		{
			var channelResolved = await client.Contacts_ResolveUsername(channelUserName);
			if (channelResolved.peer.ID != channelId)
				throw new Exception($"{channelUserName} has changed channel ID ?!");
			channelAccessHash = client.GetAccessHashFor<Channel>(channelId);
			if (channelAccessHash == 0)
				throw new Exception("No access hash was automatically collected !? (shouldn't happen)");
		}

		var channel = (await client.GetFullChat(new InputChannel(channelId, channelAccessHash))).chats.FirstOrDefault().Value as Channel;
		return channel;
	}
}