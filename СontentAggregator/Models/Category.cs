using MongoDB.Bson.Serialization.Attributes;

namespace СontentAggregator.Models;

[BsonIgnoreExtraElements]
public record CategoryItem
{
	public string Title { get; init; }
	public string Group { get; init; }
	public string Description { get; init; }
}