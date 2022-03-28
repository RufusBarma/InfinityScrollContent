namespace СontentAggregator.Models;

public record Category
{
	public string Title { get; init; }
	public List<Category> SubCategories { get; init; } = new();
	public List<CategoryItem> Items { get; init; } = new();
}

public record CategoryItem
{
	public string Title { get; init; }
	public string Description { get; init; }
}