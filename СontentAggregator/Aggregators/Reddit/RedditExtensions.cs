using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Reddit;
using Reddit.Controllers;
using СontentAggregator.Models;
using СontentAggregator.UrlResolver;

namespace СontentAggregator.Aggregators.Reddit;

public static class RedditExtensions
{
	public static IEnumerable<RedditLink> GetLinks(this IEnumerable<Post> posts, string group) => posts
			.Where(post => post is LinkPost)
			.Cast<LinkPost>()
			.Select(post => new RedditLink
			{
				Subreddit = post.Subreddit,
				SourceUrl = post.URL,
				UpVotes = post.UpVotes,
				UpvoteRatio = post.UpvoteRatio,
				Category = group,
				Urls = GetUrlsAsync(post.URL).Result.ToArray(),
				FullName = post.Fullname
			});

	private static async Task<IEnumerable<string>> GetUrlsAsync(string url)
	{
		if (Path.HasExtension(url))
			return new []{url};
		if (url.Contains("gfycat.com"))
		{
			var gfycat = new GfycatResolver();
			var result = await gfycat.ResolveAsync(url);
			if (!result.Any())
			{
				var redgifs = new RedGifsResolver();
				return await redgifs.ResolveAsync(url);
			}
		}
		if (url.Contains("redgifs.com"))
		{
			var resolver = new RedGifsResolver();
			return await resolver.ResolveAsync(url);
		}

		return Array.Empty<string>();
	}

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