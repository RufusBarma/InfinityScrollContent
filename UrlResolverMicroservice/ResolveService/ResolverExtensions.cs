using MongoDB.Driver;
using UrlResolverMicroservice.Models;

namespace UrlResolverMicroservice.ResolveService;

public static class ResolverExtensions
{
	public static IEnumerable<Link> GetEmptyUrls(this IMongoCollection<Link> collection)
	{
		var filter = Builders<Link>.Filter.And(Builders<Link>.Filter.In("Urls", new[] { null, Array.Empty<string>() }),
			Builders<Link>.Filter.Or(
				Builders<Link>.Filter.Eq(link => link.ErrorMessage, string.Empty),
				Builders<Link>.Filter.Exists(link => link.ErrorMessage, false)));
		return collection.Find(filter).ToEnumerable();
	}
}