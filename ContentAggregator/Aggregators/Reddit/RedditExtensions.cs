using ContentAggregator.Models;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Controllers;

namespace ContentAggregator.Aggregators.Reddit;

public static class RedditExtensions
{
	public static IEnumerable<RedditLink> GetLinks(this IEnumerable<Post> posts, string[] groups) => posts
			.Where(post => post is LinkPost)
			.Cast<LinkPost>()
			.Select(post => new RedditLink
			{
				PermaLink = "https://www.reddit.com" + post.Permalink,
				Subreddit = post.Subreddit,
				SourceUrl = post.URL,
				UpVotes = post.UpVotes,
				UpvoteRatio = post.UpvoteRatio,
				Category = groups.Append(post.Subreddit).Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray(),
				FullName = post.Fullname
			});

	public static Option<Subreddit> GetSubreddit(this RedditClient client, string name, ILogger logger)
	{
		try
		{
			var clearName = name.Substring(3);
			return client.Subreddit(clearName, over18: true).About();
		}
		catch (Exception ex)
		{
			ex.Data.Add("Subreddit", name);
			logger.LogWarning(ex, "Exception on get {Subreddit} subreddit", name);
			return Option<Subreddit>.None;
		}
	}

	public static IEnumerable<TResult> SelectSome<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Option<TResult>> selector) => 
		source
			.Select(selector)
			.Where(item => item.IsSome)
			.Select(item => item.ValueUnsafe());
}