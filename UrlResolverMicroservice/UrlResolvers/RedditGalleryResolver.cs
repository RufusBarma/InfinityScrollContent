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
		if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
			return response.StatusDescription;
		throw new NotImplementedException();
		var data = JArray.Parse(response.Content);
		var mediaParent = data[0]["data"]["children"][0]["data"]["media_metadata"];
		var urls = new List<string>();
		foreach (var media in mediaParent.Children())
		{
			var id = media.Children()["id"];
			urls.Add($"https://i.redd.it/{media.Value<string>("id")}.{media.Value<string>("m").Split("/").Last()}");
		}
		return urls.ToArray();
	}

	public bool CanResolve(string url) => url.Contains("reddit.com/gallery/");
}