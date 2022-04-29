using System.Net;
using LanguageExt;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using MoreLinq.Extensions;
using Newtonsoft.Json.Linq;
using RestSharp;
using UrlResolverMicroservice.Extensions;

namespace UrlResolverMicroservice.UrlResolvers;

public class ImgurResolver: IUrlResolver
{
	private readonly IDistributedCache _distributedCache;
	private bool _limitRich = false;
	private (Func<string, string> getEndpoint, Func<JObject, string[]> getUrls) _endpoints(string key) => key switch
	{
		"" => (hash => $"https://api.imgur.com/3/image/{hash}", data => new []{data["data"].Value<string>("mp4") ?? data["data"].Value<string>("link")}),
		"a" => (hash => $"https://api.imgur.com/3/album/{hash}/images", 
			baseData =>
			{
				var data = baseData["data"];
				if (data is JArray)
					return data.Children()
						.Select(image => image.Value<string>("mp4") ?? image.Value<string>("link"))
						.ToArray();
				else
					return data["images"].Children()
						.Select(image => image.Value<string>("mp4") ?? image.Value<string>("link"))
						.ToArray();
			}),
		"gallery" => (hash => $"https://api.imgur.com/3/gallery/album/{hash}", 
			data => data["data"]["link"]["images"]
				.Select(image => image["link"].Value<string>())
				.ToArray()),
		"r" => _endpoints("")
	};

	private readonly RestRequest _request;

	public ImgurResolver(IConfiguration configuration, IDistributedCache distributedCache)
	{
		_distributedCache = distributedCache;
		//TODO implement api limits on application side
		var clientId = configuration.GetSection("Imgur")["ClientId"];
		_request = new RestRequest(Method.GET);
		_request.AddHeader("Authorization", $"Client-ID {clientId}");
		_request.AlwaysMultipartFormData = true;
	}

	public async Task<Either<string, string[]>> ResolveAsync(string url)
	{
		var urlParts = url.Split('/').SkipUntil(part => part.Contains("imgur.com")).ToList();
		var endpointFuncs = urlParts.Count > 1 ? _endpoints(urlParts[0]) : _endpoints("");
		var client = new RestClient(endpointFuncs.getEndpoint(urlParts.Last()))
		{
			Timeout = -1
		};
		var response = await client.ExecuteGetTaskAsync(_request);
		if (int.Parse((string) response.Headers.FirstOrDefault(header => header.Name == "X-RateLimit-UserRemaining")?.Value ?? "6") <= 5)
		{
			var userReset = response.Headers.FirstOrDefault(header => header.Name == "X-RateLimit-UserReset")?.Value as string;
			var userResetSeconds = string.IsNullOrEmpty(userReset)? new TimeSpan(1, 0, 0, 0).TotalSeconds: double.Parse(userReset);
			var options = new DistributedCacheEntryOptions()
				.SetAbsoluteExpiration(TimeSpan.FromSeconds(userResetSeconds));
			await _distributedCache.SetAsync("Imgur.LimitRich", true, options);
		}
		if (response.StatusCode is not HttpStatusCode.OK)
			return response.StatusDescription;
		var data = JObject.Parse(response.Content);
		var urls = endpointFuncs.getUrls(data);
		return urls.Any()? urls: "Empty collection";
	}

	public bool CanResolve(string url) => url.Contains("imgur.com");

	public bool LimitRich() => _distributedCache.GetBooleanAsync("Imgur.LimitRich").Result;
}