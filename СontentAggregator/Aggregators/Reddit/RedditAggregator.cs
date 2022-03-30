using LanguageExt;
using LanguageExt.SomeHelp;
using LanguageExt.UnsafeValueAccess;
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
		foreach (var category in categoryItems.Take(1).Select(categoryItem => categoryItem.Title))
		{
			var position = _positions.Find(pos => pos.Title == category).FirstOrDefault();
			if (position == null)
			{
				position = new CategoryPosition {Title = category};
				_positions.InsertOne(position);
			}
			if (position.AfterEnd)
			{
				_logger.LogWarning($"{category} is end");
				//TODO replace on fetching before
				continue;
			}
			var sub = GetSubreddit(_reddit, category, _logger);
			if (sub.IsNone)
				continue;
			var subreddit = sub.ValueUnsafe();
			var posts = subreddit.Posts.GetTop(limit: 100, after: position.After);
			if (!posts.Any())
			{
				_logger.LogInformation($"{category} is end");
				position.AfterEnd = true;
				continue;
			}
			else
			{
				var newAfter = posts.Last().Fullname;
				position.After = newAfter;
				_positions.ReplaceOne(pos => pos.Title == position.Title, position, new ReplaceOptions {IsUpsert = true});
			}

			var links = posts
				.Where(post => post is LinkPost)
				.Cast<LinkPost>()
				.Select(post => new Link {Category = category, Url = post.URL});
			_linkCollection.InsertMany(links);
		}
		_logger.LogInformation("Finish RedditParser");
	}

	public void Stop()
	{
		throw new NotImplementedException();
	}

	private Option<Subreddit> GetSubreddit(RedditClient client, string category, ILogger logger)
	{
		try
		{
			var name = category.Substring(3);
			return client.Subreddit(name, over18: true).About();
		}
		catch (Exception ex)
		{
			ex.Data.Add("Subreddit", category);
			logger.LogWarning(ex, "Exception on get subreddit");
			return Option<Subreddit>.None;
		}
	}
}