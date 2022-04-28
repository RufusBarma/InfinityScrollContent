using ContentAggregator.Models;

namespace ContentAggregator.CategoriesAggregators;

public interface ICategoriesAggregator
{
	public Task<IEnumerable<CategoryItem>> GetCategories();
}