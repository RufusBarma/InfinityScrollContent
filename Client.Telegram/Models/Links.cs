using MongoDB.Bson.Serialization.Attributes;

namespace Client.Telegram.Models;

[BsonIgnoreExtraElements]
public record Link
{
	public string SourceUrl { get; init; }
	public string PermaLink { get; init; }
	public string Description { get; init; }
	public string[] Urls { get; set; }
	public string ErrorMessage { get; set; }
	public int UpVotes { get; init; }
	public string[] Category { get; init; }
}