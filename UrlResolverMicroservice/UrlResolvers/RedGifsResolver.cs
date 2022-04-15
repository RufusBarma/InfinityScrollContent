using System.Net;
using LanguageExt;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace UrlResolverMicroservice.UrlResolvers;

public class RedGifsResolver : IUrlResolver
{
	private readonly IRestRequest _request = new RestRequest(Method.GET).AddHeader("accept", "application/json");

	public async Task<Either<string, string[]>> ResolveAsync(string url)
	{
		//TODO parse gallery
		var id = url.Remove(0, url.LastIndexOf('/') + 1).ToLower();
		var client = new RestClient($"https://api.redgifs.com/v2/gifs/{id}");
		var response = await client.ExecuteGetTaskAsync(_request);
		if (response.StatusCode is not HttpStatusCode.OK)
			return response.StatusDescription;
		var data = JObject.Parse(response.Content);
		var urls = data["gif"]["urls"];
		var resultUrl = (urls["hd"] ?? urls["sd"]).Value<string>();
		return new[] { resultUrl };
	}

	public bool CanResolve(string url) => url.Contains("redgifs.com");

	public bool LimitRich() => false;
}