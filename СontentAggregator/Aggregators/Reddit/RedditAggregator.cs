using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Reddit;
using СontentAggregator.Models;

namespace СontentAggregator.Aggregators.Reddit;

public class RedditAggregator: IAggregator
{
	private readonly RedditClient _reddit;
	private List<Category> _categories;
	private MongoClient _clientDb;

	public RedditAggregator(IConfiguration config, RedditCategoriesAggregator categoriesAggregator)
	{
		var appId = config["app_id_reddit"];
		var refreshToken = config["refresh_token_reddit"];
		var appSecret = config["app_secret_reddit"];
		var accessToken = config["access_token_reddit"];
		_reddit = new RedditClient(appId, refreshToken, appSecret, accessToken);
		_clientDb = new MongoClient(config.GetConnectionString("DefaultConnection"));
		_categories = categoriesAggregator.GetCategories();
	}

	public void Start()
	{
		var testCategory = _categories.First(category => category.Items.Any());
		var name = testCategory.Items[1].Title.Substring(3);
		var subreddit = _reddit.Subreddit(name, over18: true);
		var posts = subreddit.Posts.GetTop(limit: 1);
		Console.WriteLine(posts.FirstOrDefault()?.Author);
	}

	public void Stop()
	{
		throw new NotImplementedException();
	}
}