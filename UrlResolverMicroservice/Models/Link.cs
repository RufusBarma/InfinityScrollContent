using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UrlResolverMicroservice.Models;

public enum LinkType
{
	None,
	Video,
	Img,
	Gif,
}

[BsonIgnoreExtraElements]
public record Link
{
	public ObjectId _id { get; init; }
	public string SourceUrl { get; init; }
	public string[] Urls { get; set; }
	public LinkType Type { get; set; }
	public bool IsGallery { get; set; }
	public string ErrorMessage { get; set; }
}