using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UrlResolverMicroservice.Models;

public enum LinkType
{
	None,
	Video,
	Img,
	Gif,
	Album
}

[BsonIgnoreExtraElements]
public record Link
{
	public BsonObjectId _id { get; init; }
	public string SourceUrl { get; init; }
	public string[] Urls { get; set; }
	public LinkType Type { get; set; }
	public bool IsGallery { get; set; }
	public bool DeleteMe { get; set; }
}