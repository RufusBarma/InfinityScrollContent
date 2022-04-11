using Bot.Telegram.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bot.Telegram.Bot;

public class MailingQueue
{
	private IMongoCollection<Link> _linkCollection;
	private IMongoCollection<Channel> _channelCollection;

	public MailingQueue(IMongoClient dbClient)
	{
		var redditDb = dbClient.GetDatabase("Reddit");
		_linkCollection = redditDb.GetCollection<Link>("Links");
		_channelCollection = redditDb.GetCollection<Channel>("Channels");
	}
	
	public async Task StartMailing()
	{
		
	}
}