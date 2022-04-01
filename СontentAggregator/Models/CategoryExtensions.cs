using MongoDB.Driver;

namespace СontentAggregator.Models;

public static class CategoryExtensions
{
	public static async Task<CategoryPosition> FindOrCreate(this IMongoCollection<CategoryPosition> positions, string category)
	{
		var position = await (await positions.FindAsync(pos => pos.Title == category)).FirstOrDefaultAsync();
		if (position != null) return position;

		position = new CategoryPosition {Title = category};
		await positions.InsertOneAsync(position);
		return position;
	}
}