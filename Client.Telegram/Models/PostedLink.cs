using MongoDB.Bson.Serialization.Attributes;

namespace Client.Telegram.Models;

[BsonIgnoreExtraElements]
public record PostedLink
{
	public string SourceUrl { get; init; }
	public long ChannelId { get; init; }
	[BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
	public DateTime PostDate { get; init; }
}