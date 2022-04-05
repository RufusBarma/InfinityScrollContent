using Microsoft.Extensions.Configuration;
using MoreLinq.Extensions;
using RestSharp;

namespace UrlResolverMicroservice.UrlResolvers;

public class ImgurResolver: IUrlResolver
{
	private Dictionary<string, Func<string, string>> _endpoints = new()
	{
		{"", hash => $"https://api.imgur.com/3/image/{hash}"},
		{"a", hash => $"https://api.imgur.com/3/album/{hash}/images"},
		{"gallery", hash => $"https://api.imgur.com/3/gallery/album/{hash}"}
	};

	private readonly RestRequest _request;

	public ImgurResolver(IConfiguration configuration)
	{
		var clientId = configuration.GetSection("Imgur")["ClientId"];
		_request = new RestRequest(Method.GET);
		_request.AddHeader("Authorization", $"Client-ID {clientId}");
		_request.AlwaysMultipartFormData = true;
	}

	public async Task<IEnumerable<string>> ResolveAsync(string url)
	{
		var urlParts = url.Split('/').SkipUntil(part => part != "imgur.com");
		var client = new RestClient("https://api.imgur.com/3/album/{{albumHash}}/images")
		{
			Timeout = -1
		};
		var response = await client.ExecuteGetTaskAsync(_request);
		throw new NotImplementedException();
	}

	public bool CanResolve(string url) => url.Contains("imgur.com");
}