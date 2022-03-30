using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reddit;
using Reddit.Controllers;
using Reddit.Exceptions;
using СontentAggregator.Models;

namespace СontentAggregator.Aggregators.Reddit;

public class RedditAggregator: IAggregator
{
	private readonly RedditClient _reddit;
	private List<Category> _categories;
	private IMongoCollection<Link> _linkCollection;
	private IMongoCollection<CategoryPosition> _positions;
	private ILogger _logger;

	public RedditAggregator(IConfiguration config, RedditCategoriesAggregator categoriesAggregator, ILogger<RedditAggregator> logger)
	{
		_logger = logger;
		var appId = config["app_id_reddit"];
		var refreshToken = config["refresh_token_reddit"];
		var appSecret = config["app_secret_reddit"];
		var accessToken = config["access_token_reddit"];
		_reddit = new RedditClient(appId, refreshToken, appSecret, accessToken);
		var redditDb = new MongoClient(config.GetConnectionString("DefaultConnection")).GetDatabase("Reddit");
		_linkCollection = redditDb.GetCollection<Link>("Links");
		_positions = redditDb.GetCollection<CategoryPosition>("CategoryPositions");
		_categories = categoriesAggregator.GetCategories();
	}

	private IEnumerable<CategoryItem> GetAllItems(Category category)
	{
		return category.SubCategories.SelectMany(GetAllItems).Concat(category.Items);
	}

	public void Start()
	{
		var categoryItems = _categories.SelectMany(GetAllItems).ToList();
		foreach (var category in categoryItems.Skip(120).Take(20).Select(categoryItem => categoryItem.Title))
		{
			var position = _positions.Find(pos => pos.Title == category).FirstOrDefault();
			if (position == null)
			{
				position = new CategoryPosition {Title = category};
				_positions.InsertOne(position);
			}
			var name = category.Substring(3);
			Subreddit subreddit;
			try
			{
				subreddit = _reddit.Subreddit(name, over18: true).About();
			}
			catch (RedditNotFoundException ex)
			{
				_logger.LogError(ex, ex.Message);
				continue;
			}
			if (subreddit.Created == default)
			{
				_logger.LogWarning($"{category} is banned or not exist");
				break;
			}
			if (position.AfterEnd)
			{
				_logger.LogWarning($"{category} is end");
				continue;
			}
			var posts = subreddit.Posts.GetTop(limit: 100, after: position.After);
			var newAfter = posts.Last().Fullname;
			if (position.After == newAfter)
			{
				_logger.LogInformation($"{category} is end");
				position.AfterEnd = true;
			}
			else
			{
				position.After = newAfter;
				_positions.ReplaceOne(pos => pos.Title == position.Title, position, new ReplaceOptions {IsUpsert = true});
			}
			var links = posts.Select(post =>
			{
				var url = post switch
				{
					LinkPost linkPost => linkPost.URL,
					SelfPost selfPost => selfPost.About().SelfText,
					_ => throw new ArgumentOutOfRangeException(nameof(post), post, null)
				};
				return new Link {Category = category, Url = url};
			});
			_linkCollection.InsertMany(links);
		}
		_logger.LogInformation("Finish RedditParser");
	}

	public void Stop()
	{
		throw new NotImplementedException();
	}
}