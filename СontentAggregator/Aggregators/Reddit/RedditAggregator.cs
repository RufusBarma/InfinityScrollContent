using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reddit;
using Reddit.Controllers;
using СontentAggregator.Models;

namespace СontentAggregator.Aggregators.Reddit;

public class RedditAggregator: IAggregator
{
	private readonly RedditClient _reddit;
	private List<Category> _categories;
	private IMongoCollection<Link> _linkCollection;
	private IMongoCollection<CategoryPosition> _positions;
	private ILogger _logger;
	private Task _aggregatorTask;
	private CancellationTokenSource _cancelTokenSource = new ();
	private CancellationToken _cancellation;

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

	public Task Start()
	{
		_cancellation = _cancelTokenSource.Token;
		_aggregatorTask = RunAggregator(_cancellation);
		return _aggregatorTask;
	}

	public void Stop()
	{
		_cancelTokenSource.Cancel();
	}

	private async Task RunAggregator(CancellationToken cancellationToken)
	{
		var categoryItems = _categories.SelectMany(category => category.GetAllItems()).ToList();
		foreach (var links in categoryItems.Take(1)
			         .Select(categoryItem => categoryItem.Title)
			         .Select(category => _reddit.GetSubreddit(category, _logger))
			         .Where(subreddit => subreddit.IsSome)
			         .Select(subreddit => subreddit.ValueUnsafe())
			         .Select(async subreddit => await GetPosts(subreddit, await _positions.FindOrCreate(subreddit.Name)))
			         .Select(async posts => (await posts).GetLinks()))
		{
			if (cancellationToken.IsCancellationRequested)
			{
				_logger.LogInformation("Cancel reddit aggregator");
				return;
			}

			var link = await links;
			await _linkCollection.InsertManyAsync(link);
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
			var newAfter = posts.Last().Fullname;
			position.After = newAfter;
			await _positions.ReplaceOneAsync(pos => pos.Title == position.Title, position, new ReplaceOptions {IsUpsert = true});
		}

		return posts;
	}
}