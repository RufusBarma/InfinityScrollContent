using MongoDB.Bson.Serialization.Attributes;

namespace СontentAggregator.Models;

public record CategoryPosition
{
	[BsonId]
	public string Title { get; init; } = "";
	public string Before { get; set; } = "";
	public string After { get; set; } = "";
	public bool AfterEnd { get; set; }
	public bool BeforeEnd { get; set; }
}