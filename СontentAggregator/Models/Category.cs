using MongoDB.Bson.Serialization.Attributes;

namespace СontentAggregator.Models;

[BsonIgnoreExtraElements]
public record Category
{
	public string Title { get; init; }
	public List<Category> SubCategories { get; init; } = new();
	public List<CategoryItem> Items { get; init; } = new();
}

[BsonIgnoreExtraElements]
public record CategoryItem
{
	public string Title { get; init; }
	public string Description { get; init; }
}