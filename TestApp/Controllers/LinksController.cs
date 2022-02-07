using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace TestApp.Controllers;

public record Link
{
    [JsonIgnore, IgnoreDataMember] public ObjectId Id { get; set; }
    public string Value { get; set; }
}

[ApiController]
[Route("[controller]")]
public class LinksController: ControllerBase
{
    [HttpGet]
    public async Task<JsonResult> Get(int count=1, bool looped = false)
    {
        const string connectionString = "mongodb://root:example@localhost:27017";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("MyTestProject");
        var collection = database.GetCollection<BsonDocument>("links");
        var documents = await collection.Find(new BsonDocument()).ToListAsync();
        if (documents.Count == 1)
        {
            await collection.InsertOneAsync(new Link{Value = "https://thumbs.gfycat.com/SameLiveKoala-mobile.mp4"}.ToBsonDocument());
        }
        var links = documents.Select(doc => BsonSerializer.Deserialize<Link>(doc)).Take(count).ToImmutableList();

        if (looped)
            while (count > links.Count)
                links = links.Concat(links).Take(count).ToImmutableList();
        return new JsonResult(links);
    }
}