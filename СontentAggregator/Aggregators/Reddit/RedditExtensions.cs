using LanguageExt;
using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Controllers;
using СontentAggregator.Models;

namespace СontentAggregator.Aggregators.Reddit;

public static class RedditExtensions
{
	public static IEnumerable<Link> GetLinks(this IEnumerable<Post> posts)
	{
		return posts
			.Where(post => post is LinkPost)
			.Cast<LinkPost>()
			.Select(post => new Link {Category = post.Subreddit, Url = post.URL});
	}

	public static Option<Subreddit> GetSubreddit(this RedditClient client, string category, ILogger logger)
	{
		try
		{
			var name = category.Substring(3);
			return client.Subreddit(name, over18: true).About();
		}
		catch (Exception ex)
		{
			ex.Data.Add("Subreddit", category);
			logger.LogWarning(ex, "Exception on get {Subbreddit} subreddit", category);
			return Option<Subreddit>.None;
		}
	}
}