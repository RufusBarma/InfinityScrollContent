using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MoreLinq;
using Reddit;
using Reddit.Controllers;
using СontentAggregator.Models;

namespace СontentAggregator.Aggregators.Reddit;

public class RedditAggregator: IAggregator
{
	private readonly RedditClient _reddit;
	private List<CategoryItem> _categories;
	private IMongoCollection<RedditLink> _linkCollection;
	private IMongoCollection<CategoryPosition> _positions;
	private ILogger _logger;
	private Task _aggregatorTask;

	public RedditAggregator(IConfiguration config, RedditCategoriesAggregator categoriesAggregator, ILogger<RedditAggregator> logger)
	{
		_logger = logger;
		var appId = config["app_id_reddit"];
		var refreshToken = config["refresh_token_reddit"];
		var appSecret = config["app_secret_reddit"];
		var accessToken = config["access_token_reddit"];
		_reddit = new RedditClient(appId, refreshToken, appSecret, accessToken);
		var redditDb = new MongoClient(config.GetConnectionString("DefaultConnection")).GetDatabase("Reddit");
		_linkCollection = redditDb.GetCollection<RedditLink>("Links");
		_positions = redditDb.GetCollection<CategoryPosition>("CategoryPositions");
		_categories = categoriesAggregator.GetCategories();
	}

	public Task Start(CancellationToken cancellationToken)
	{
		_aggregatorTask = RunAggregator(cancellationToken);
		return _aggregatorTask;
	}

	private async Task RunAggregator(CancellationToken cancellationToken)
	{
		var categoryItems = _categories.Shuffle().Take(5);
		foreach (var linksTask in categoryItems
			         .Select(category => (category, Subreddit: _reddit.GetSubreddit(category.Title, _logger)))
			         .Where(tuple => tuple.Subreddit.IsSome)
			         .Select(tuple => (tuple.category, Subreddit: tuple.Subreddit.ValueUnsafe()))
			         .Select(async tuple => (tuple.category, (await GetPosts(tuple.Subreddit, await _positions.FindOrCreate(tuple.Subreddit.Name)))))
			         .Select(posts => posts.Item2.GetLinks(posts.category.Group)))
		{
			var links = (await linksTask).ToList();
			if (links.Any())
				await _linkCollection.InsertManyAsync(links);
			if (cancellationToken.IsCancellationRequested)
			{
				_logger.LogInformation("Cancel reddit aggregator");
				return;
			}
		}
		_logger.LogInformation("Finish RedditParser");
	}

	private async Task<IEnumerable<Post>> GetPosts(Subreddit subreddit, CategoryPosition position)
	{
		var posts = subreddit.Posts.GetTop(limit: 100, after: position.After);
		if (!posts.Any())
		{
			_logger.LogInformation("Subreddit is end: {SubredditName}", subreddit.Name);
			position.AfterEnd = true;
		}
		else
		{
			var newBefore = posts.First().Fullname;
			var newAfter = posts.Last().Fullname;
			position.After = newAfter;
			var filter = Builders<CategoryPosition>.Filter.Eq(pos => pos.Title, position.Title);
			var update = Builders<CategoryPosition>.Update.Set(pos => pos.After, newAfter).SetOnInsert(pos => pos.Before, newBefore);
			await _positions.UpdateOneAsync(filter, update, new UpdateOptions {IsUpsert = true});
		}

		return posts;
	}
}