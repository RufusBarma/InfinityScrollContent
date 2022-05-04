using MongoDB.Bson.Serialization.Attributes;

namespace Client.Telegram.Models;

[BsonIgnoreExtraElements]
public record LinkWithPosted : Link
{
	public PostedLink[] PostedLinks { get; init; }
}