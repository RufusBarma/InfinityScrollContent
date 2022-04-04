using System.Net;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace UrlResolverMicroservice.UrlResolvers;

public class GfycatResolver : IUrlResolver
{
	private readonly IRestRequest _request = new RestRequest(Method.GET).AddHeader("accept", "application/json");

	public async Task<IEnumerable<string>> ResolveAsync(string url)
	{
		//TODO parse gallery
		var id = url.Remove(0, url.LastIndexOf('/') + 1);
		var client = new RestClient($"https://api.gfycat.com/v1/gfycats/{id}");
		var response = await client.ExecuteGetTaskAsync(_request);
		if (response.StatusCode == HttpStatusCode.NotFound)
			return Array.Empty<string>();
		if (response.StatusCode == HttpStatusCode.Gone)
			return Array.Empty<string>();
		var data = JObject.Parse(response.Content);
		var resultUrl = data["gfyItem"]["mp4Url"].Value<string>();
		if (string.IsNullOrEmpty(resultUrl))
			return Array.Empty<string>();
		return new[] { resultUrl };
	}

	public bool CanResolve(string url) => url.Contains("gfycat.com");
}