using System.Net;
using LanguageExt;
using Microsoft.Extensions.Configuration;
using MoreLinq.Extensions;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace UrlResolverMicroservice.UrlResolvers;

public class ImgurResolver: IUrlResolver
{
	private Dictionary<string, (Func<string, string> getEndpoint, Func<JObject, string[]> getUrls)> _endpoints = new()
	{
		{"", (hash => $"https://api.imgur.com/3/image/{hash}", data => new []{data["data"]["link"].Value<string>()})},
		{"a", (hash => $"https://api.imgur.com/3/album/{hash}/images", 
			data => data["data"].Children()
				.Select(image => image.Value<string>("mp4") ?? image.Value<string>("link"))
				.ToArray())},
		{"gallery", (hash => $"https://api.imgur.com/3/gallery/album/{hash}", 
			data => data["data"]["link"]["images"]
				.Select(image => image["link"].Value<string>())
				.ToArray())}
	};

	private readonly RestRequest _request;

	public ImgurResolver(IConfiguration configuration)
	{
		//TODO implement api limits on application side
		var clientId = configuration.GetSection("Imgur")["ClientId"];
		_request = new RestRequest(Method.GET);
		_request.AddHeader("Authorization", $"Client-ID {clientId}");
		_request.AlwaysMultipartFormData = true;
	}

	public async Task<Either<string, string[]>> ResolveAsync(string url)
	{
		var urlParts = url.Split('/').SkipUntil(part => part.Contains("imgur.com")).ToList();
		var endpointFuncs = urlParts.Count > 1 ? _endpoints[urlParts[0]] : _endpoints[""];
		var client = new RestClient(endpointFuncs.getEndpoint(urlParts.Last()))
		{
			Timeout = -1
		};
		var response = await client.ExecuteGetTaskAsync(_request);
		if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
			return response.StatusDescription;
		var data = JObject.Parse(response.Content);
		var urls = endpointFuncs.getUrls(data);
		return urls.Any()? urls: "Empty collection";
	}

	public bool CanResolve(string url) => url.Contains("imgur.com");
}