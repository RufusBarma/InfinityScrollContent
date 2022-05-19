using MongoDB.Bson.Serialization.Attributes;

namespace Client.Telegram.SenderSettings;

[BsonIgnoreExtraElements]
public class SenderSettings
{
	public long ChannelId { get; init; }
	public string ChannelUsername { get; init; } = "";
	public string[] Categories { get; init; }
	public string[] ExceptCategories { get; init; }
	public string Cron { get; init; }
}