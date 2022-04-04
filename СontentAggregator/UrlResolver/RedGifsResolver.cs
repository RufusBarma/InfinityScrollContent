using System.Net;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace СontentAggregator.UrlResolver;

public class RedGifsResolver: IUrlResolver
{
	private readonly IRestRequest _request = new RestRequest(Method.GET).AddHeader("accept", "application/json");

	public async Task<IEnumerable<string>> ResolveAsync(string url)
	{
		//TODO parse gallery
		var id = url.Remove(0, url.LastIndexOf('/') + 1).ToLower();
		var client = new RestClient($"https://api.redgifs.com/v2/gifs/{id}");
		var response = await client.ExecuteGetTaskAsync(_request);
		if (response.StatusCode == HttpStatusCode.NotFound)
			return new[] {"NotFound"};
		if (response.StatusCode == HttpStatusCode.Gone)
			return new[] {"Deleted"};
		var data = JObject.Parse(response.Content);
		var urls = data["gif"]["urls"];
		var resultUrl = (urls["hd"] ?? urls["sd"]).Value<string>();
		if (string.IsNullOrEmpty(resultUrl))
			return new[] {"deleteme"};
		return new []{resultUrl};
	}
}