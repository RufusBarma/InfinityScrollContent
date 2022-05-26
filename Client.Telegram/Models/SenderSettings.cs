using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Client.Telegram.SenderSettings;

[BsonIgnoreExtraElements]
public class SenderSettings
{
	[BsonRepresentation(BsonType.ObjectId)]
	public string _id { get; init; }
	public long ChannelId { get; init; }
	public string ChannelUsername { get; init; } = "";
	public string[] Categories { get; init; }
	public string[] ExceptCategories { get; init; }
	public string Cron { get; init; }
	public bool OnlyFromCategories { get; init; }
}