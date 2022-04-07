using MongoDB.Bson.Serialization.Attributes;

namespace ContentAggregator.Models;

public record CategoryPosition
{
	[BsonId]
	public string Title { get; init; } = "";
	public string Before { get; set; } = "";
	public string After { get; set; } = "";
	public bool AfterEnd { get; set; }
}