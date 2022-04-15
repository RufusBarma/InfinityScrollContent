using System.Net;
using LanguageExt;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace UrlResolverMicroservice.UrlResolvers;

public class RedditGalleryResolver: IUrlResolver
{
	public async Task<Either<string, string[]>> ResolveAsync(string url)
	{
		var urlJson = $"{url.Replace("gallery", "comments")}.json";
		var client = new RestClient(urlJson);
		var response = await client.ExecuteGetTaskAsync(new RestRequest());
		if (response.StatusCode is not HttpStatusCode.OK)
			return response.StatusDescription;
		var data = JArray.Parse(response.Content);
		var mediaParent = data[0]["data"]["children"][0]["data"]["media_metadata"];
		if (mediaParent == null)
			return "Deleted by user";
		var urls = new List<string>();
		foreach (var media in mediaParent.Children<JProperty>())
		{
			var id = media.Value["id"].Value<string>();
			var type = media.Value["m"].Value<string>().Split("/").Last();
			urls.Add($"https://i.redd.it/{id}.{type}");
		}
		urls.Reverse();
		return urls.ToArray();
	}

	public bool CanResolve(string url) => url.Contains("reddit.com/gallery/");

	public bool LimitRich() => false;
}