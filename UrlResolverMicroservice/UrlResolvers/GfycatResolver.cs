using System.Net;
using LanguageExt;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace UrlResolverMicroservice.UrlResolvers;

public class GfycatResolver : IUrlResolver
{
	private readonly RedGifsResolver _redGifsResolver;
	private readonly IRestRequest _request = new RestRequest(Method.GET).AddHeader("accept", "application/json");

	public GfycatResolver(RedGifsResolver redGifsResolver)
	{
		_redGifsResolver = redGifsResolver;
	}

	public async Task<Either<string, string[]>> ResolveAsync(string url)
	{
		//TODO parse gallery
		var id = url.Remove(0, url.LastIndexOf('/') + 1);
		var client = new RestClient($"https://api.gfycat.com/v1/gfycats/{id}");
		var response = await client.ExecuteGetTaskAsync(_request);
		if (response.StatusCode is not HttpStatusCode.OK)
			return await GetFromRedGifs(url);
		var data = JObject.Parse(response.Content);
		var resultUrl = data["gfyItem"]["mp4Url"].Value<string>();
		if (string.IsNullOrEmpty(resultUrl))
			return "Urls not found";
		return new[] { resultUrl };
	}

	public bool CanResolve(string url) => url.Contains("gfycat.com");

	private Task<Either<string, string[]>> GetFromRedGifs(string url) => _redGifsResolver.ResolveAsync(url);

	public bool LimitRich() => false;
}