using Microsoft.Extensions.Configuration;
using Reddit;
using Reddit.Controllers;

namespace СontentAggregator.Aggregators.Reddit;

public class RedditAggregator: IAggregator
{
	private readonly RedditClient _reddit;

	public RedditAggregator(IConfiguration config)
	{
		var appId = config["app_id_reddit"];
		var refreshToken = config["refresh_token_reddit"];
		var appSecret = config["app_secret_reddit"];
		var accessToken = config["access_token_reddit"];
		_reddit = new RedditClient(appId, refreshToken, appSecret, accessToken);
	}

	public void Start()
	{
		var subreddit = _reddit.Subreddit("all", over18: true);
		List<Post> posts = subreddit.About().Posts.Top;
	}

	public void Stop()
	{
		throw new NotImplementedException();
	}
}