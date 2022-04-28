using ContentAggregator.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ContentAggregator.CategoriesAggregators.Reddit;

public class RedditCategoriesAggregator: ICategoriesAggregator
{
	private MongoClient _clientDb;
	private readonly RedditHtmlCategoriesAggregator _htmlCategoriesAggregator;
	private readonly RedditCsvCategoriesEnricher _csvCategoriesEnricher;

	public RedditCategoriesAggregator(IConfiguration config,
		RedditHtmlCategoriesAggregator htmlCategoriesAggregator,
		RedditCsvCategoriesEnricher csvCategoriesEnricher)
	{
		_htmlCategoriesAggregator = htmlCategoriesAggregator;
		_csvCategoriesEnricher = csvCategoriesEnricher;
		_clientDb = new MongoClient(config.GetConnectionString("DefaultConnection"));
	}

	public async Task<IEnumerable<CategoryItem>> GetCategories()
	{
		var database = _clientDb.GetDatabase("Reddit");
		var collection = database.GetCollection<CategoryItem>("Categories");
		var documentsCount = await collection.CountDocumentsAsync(doc => true);
		var categories = collection.Aggregate().ToEnumerable();
		if (documentsCount == 0)
		{
			categories = await _htmlCategoriesAggregator.GetCategories();
			categories = await _csvCategoriesEnricher.Enrich(categories);
			await collection.InsertManyAsync(categories);
		}
		return categories;
	}
}