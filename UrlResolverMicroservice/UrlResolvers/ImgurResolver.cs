using System.Net;
using LanguageExt;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using Newtonsoft.Json.Linq;
using RestSharp;
using UrlResolverMicroservice.Extensions;

namespace UrlResolverMicroservice.UrlResolvers;

public class ImgurResolver: IUrlResolver
{
	private readonly IDistributedCache _distributedCache;
	private readonly ILogger<ImgurResolver> _logger;
	private bool _limitRich = false;

	private static (Func<string, string> getEndpoint, Func<JObject, string[]> getUrls) ApiImage() => 
		(hash => $"https://api.imgur.com/3/image/{hash}", data => new[] { data["data"].Value<string>("mp4") ?? data["data"].Value<string>("link") });

	private static (Func<string, string> getEndpoint, Func<JObject, string[]> getUrls) ApiAlbum() => 
		(hash => $"https://api.imgur.com/3/album/{hash}/images", 
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
			});

	private static (Func<string, string> getEndpoint, Func<JObject, string[]> getUrls) ApiGallery() => 
		(hash => $"https://api.imgur.com/3/gallery/album/{hash}", 
			data => data["data"]["images"] != null
				? ApiAlbum().getUrls(data)
				: data["data"]["link"]["images"]
					.Select(image => image["link"].Value<string>())
					.ToArray());

	private Dictionary<string, (Func<string, string> getEndpoint, Func<JObject, string[]> getUrls)> _endpoints = new()
	{
		{"", ApiImage()},
		{"a", ApiAlbum()},
		{"gallery", ApiGallery()},
		{"r", ApiImage()},
	};

	private readonly RestRequest _request;

	public ImgurResolver(IConfiguration configuration, IDistributedCache distributedCache, ILogger<ImgurResolver> logger)
	{
		_distributedCache = distributedCache;
		_logger = logger;
		//TODO implement api limits on application side
		var clientId = configuration.GetSection("Imgur")["ClientId"];
		_request = new RestRequest(Method.GET);
		_request.AddHeader("Authorization", $"Client-ID {clientId}");
		_request.AlwaysMultipartFormData = true;
	}

	public async Task<Either<string, string[]>> ResolveAsync(string url)
	{
		var urlParts = url.Split('/', StringSplitOptions.RemoveEmptyEntries).SkipUntil(part => part.Contains("imgur.com")).ToList();
		var apiKey = urlParts.Count > 1 ? urlParts[0] : "";
		if (!_endpoints.ContainsKey(apiKey))
		{
			_logger.LogError("Exception on get endpoint {url}", url);
			return "Skip";
		}
		var endpointFunc = _endpoints[apiKey];
		var client = new RestClient(endpointFunc.getEndpoint(Path.GetFileNameWithoutExtension(urlParts.Last())))
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
			if (string.IsNullOrEmpty(response.StatusDescription))
				throw new Exception("Status description is null");
			else 
				return response.StatusDescription;
		var data = JObject.Parse(response.Content);
		var urls = endpointFunc.getUrls(data);
		return urls.Any() ? urls : "Empty collection";
	}

	public bool CanResolve(string url) => url.Contains("imgur.com");

	public bool LimitRich() => _distributedCache.GetBooleanAsync("Imgur.LimitRich").Result;
}