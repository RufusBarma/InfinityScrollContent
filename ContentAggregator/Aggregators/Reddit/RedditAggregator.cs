using ContentAggregator.CategoriesAggregators;
using ContentAggregator.Models;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MoreLinq.Extensions;
using Reddit;
using Reddit.Controllers;

namespace ContentAggregator.Aggregators.Reddit;

public class RedditAggregator: IAggregator
{
	private readonly RedditClient _reddit;
	private ICategoriesAggregator _categoriesAggregator;
	private IMongoCollection<RedditLink> _linkCollection;
	private IMongoCollection<CategoryPosition> _positions;
	private ILogger _logger;
	private Task _aggregatorTask;

	public RedditAggregator(IConfiguration config, ICategoriesAggregator categoriesAggregator, ILogger<RedditAggregator> logger, IMongoDatabase database)
	{
		_logger = logger;
		var appId = config.GetRequiredSection("app_id_reddit").Value;
		var refreshToken = config.GetRequiredSection("refresh_token_reddit").Value;
		var appSecret = config.GetRequiredSection("app_secret_reddit").Value;
		var accessToken = config.GetRequiredSection("access_token_reddit").Value;
		_reddit = new RedditClient(appId, refreshToken, appSecret, accessToken);
		_linkCollection = database.GetCollection<RedditLink>("Links");
		_positions = database.GetCollection<CategoryPosition>("CategoryPositions");
		_categoriesAggregator = categoriesAggregator;
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		var categories = (await _categoriesAggregator.GetCategories()).ToList();
		await RunAggregator(categories, cancellationToken);
	}

	private async Task RunAggregator(List<CategoryItem> categories, CancellationToken cancellationToken)
	{
		var categoryItems = categories.Shuffle();
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
		var after = position.AfterEnd ? null : position.After;
		var before = position.AfterEnd ? position.Before : null;
		var posts = subreddit.Posts.GetTop(limit: 100, after: after, before: before);
		if (!position.AfterEnd)
		{
			if (!posts.Any())
			{
				_logger.LogInformation("Subreddit is end: {SubredditName}", subreddit.Name);
				position.AfterEnd = true;
			}
			else
			{
				position.After = posts.Last().Fullname;
				if (string.IsNullOrEmpty(position.Before))
					position.Before = posts.First().Fullname;
			}
		}
		else
		{
			if (!posts.Any())
			{
				_logger.LogInformation("No updates for subreddit: {SubredditName}", subreddit.Name);
				return posts;
			}
			position.Before = posts.First().Fullname;
		}
		var filter = Builders<CategoryPosition>.Filter.Eq(pos => pos.Title, position.Title);
		var update = Builders<CategoryPosition>.Update
			.Set(pos => pos.After, position.After)
			.Set(pos => pos.Before, position.Before)
			.Set(pos => pos.AfterEnd, position.AfterEnd);
		await _positions.UpdateOneAsync(filter, update, new UpdateOptions {IsUpsert = true});

		return posts;
	}
}