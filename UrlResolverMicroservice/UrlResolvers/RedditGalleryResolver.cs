using System.Net;
using LanguageExt;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Linq;
using RestSharp;
using UrlResolverMicroservice.Extensions;

namespace UrlResolverMicroservice.UrlResolvers;

public class RedditGalleryResolver: IUrlResolver
{
	private readonly IDistributedCache _distributedCache;

	public RedditGalleryResolver(IDistributedCache distributedCache)
	{
		_distributedCache = distributedCache;
	}

	public async Task<Either<string, string[]>> ResolveAsync(string url)
	{
		var urlJson = $"{url.Replace("gallery", "comments")}.json";
		var client = new RestClient(urlJson);
		var response = await client.ExecuteGetTaskAsync(new RestRequest());

		if (int.Parse((string) response.Headers.FirstOrDefault(header => header.Name == "x-ratelimit-remaining")?.Value ?? "0") <= 1)
		{
			int.TryParse((string) response.Headers.FirstOrDefault(header => header.Name == "x-ratelimit-reset")?.Value, out var reset);
			var options = new DistributedCacheEntryOptions()
				.SetAbsoluteExpiration(TimeSpan.FromSeconds(reset));
			await _distributedCache.SetAsync("RedditGallery.LimitRich", true, options);
		}
		if (response.StatusCode is not HttpStatusCode.OK)
			return response.StatusDescription;
		var data = JArray.Parse(response.Content);
		var mediaParent = data[0]["data"]["children"][0]["data"]["media_metadata"];
		if (mediaParent == null)
			return "Deleted by user";
		var urls = new List<string>();
		foreach (var media in mediaParent.Children<JProperty>())
		{
			if (media.Value["status"].Value<string>() == "failed")
				continue;
			var id = media.Value["id"].Value<string>();
			var type = media.Value["m"].Value<string>().Split("/").Last();
			urls.Add($"https://i.redd.it/{id}.{type}");
		}
		urls.Reverse();
		return urls.ToArray();
	}

	public bool CanResolve(string url) => url.Contains("reddit.com/gallery/");

	public bool LimitRich() => _distributedCache.GetBooleanAsync("RedditGallery.LimitRich").Result;
}