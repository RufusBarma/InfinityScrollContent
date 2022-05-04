using ContentAggregator.Models;
using MongoDB.Driver;

namespace ContentAggregator.CategoriesAggregators.Reddit;

public class RedditCategoriesAggregator: ICategoriesAggregator
{
	private readonly RedditHtmlCategoriesAggregator _htmlCategoriesAggregator;
	private readonly RedditCsvCategoriesEnricher _csvCategoriesEnricher;
	private readonly IMongoCollection<CategoryItem> _collection;

	public RedditCategoriesAggregator(
		IMongoDatabase database,
		RedditHtmlCategoriesAggregator htmlCategoriesAggregator,
		RedditCsvCategoriesEnricher csvCategoriesEnricher)
	{
		_htmlCategoriesAggregator = htmlCategoriesAggregator;
		_csvCategoriesEnricher = csvCategoriesEnricher;
		_collection = database.GetCollection<CategoryItem>("Categories");
	}

	public async Task<IEnumerable<CategoryItem>> GetCategories()
	{
		var documentsCount = await _collection.CountDocumentsAsync(doc => true);
		var categories = _collection.Aggregate().ToEnumerable();
		if (documentsCount == 0)
		{
			categories = await _htmlCategoriesAggregator.GetCategories();
			categories = await _csvCategoriesEnricher.Enrich(categories);
			await _collection.InsertManyAsync(categories);
		}
		return categories;
	}
}