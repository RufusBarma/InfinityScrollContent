using MongoDB.Bson.Serialization.Attributes;
using TL;

namespace Client.Telegram.Client;

public record SavedState
{
    [BsonId]
    public string Username { get; init; }
    public List<KeyValuePair<long, long>> Channels { get; init; } = new();
    public List<KeyValuePair<long, long>> Users { get; init; } = new();

    public void SetAccessHash(WTelegram.Client client)
    {
        Channels.ForEach(id_hash => client.SetAccessHashFor<Channel>(id_hash.Key, id_hash.Value));
        Users.ForEach(id_hash => client.SetAccessHashFor<User>(id_hash.Key, id_hash.Value));
    }
}