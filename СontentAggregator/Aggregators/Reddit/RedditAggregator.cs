using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Reddit;
using Reddit.Controllers;
using СontentAggregator.Models;

namespace СontentAggregator.Aggregators.Reddit;

public class RedditAggregator: IAggregator
{
	private readonly RedditClient _reddit;
	private List<Category> _categories;

	public RedditAggregator(IConfiguration config, RedditCategoriesAggregator categoriesAggregator)
	{
		var appId = config["app_id_reddit"];
		var refreshToken = config["refresh_token_reddit"];
		var appSecret = config["app_secret_reddit"];
		var accessToken = config["access_token_reddit"];
		_reddit = new RedditClient(appId, refreshToken, appSecret, accessToken);
		var client = new MongoClient(config.GetConnectionString("DefaultConnection"));
		var database = client.GetDatabase("Reddit");
		var collection = database.GetCollection<Category>("Categories");
		_categories = collection.Aggregate().ToList();
		var documentsCount = collection.CountDocuments(doc => true);
		if (documentsCount == 0)
		{
			_categories = categoriesAggregator.GetCategories();
			collection.InsertMany(_categories);
		}
	}

	public async void Start()
	{
		var testCategory = _categories.First(category => category.Items.Any());
		var subreddit = _reddit.Subreddit(testCategory.Items[1].Title, over18: true);
		List<Post> posts = subreddit.Posts.GetTop(limit: 5);
		foreach (var post in posts)
		{
		}
	}

	public void Stop()
	{
		throw new NotImplementedException();
	}
}