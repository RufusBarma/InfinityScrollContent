using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Client.Telegram.SenderSettings;

public class SenderSettingsFromMongoDb: ISenderSettingsFetcher
{
	private readonly ILogger<SenderSettingsFromMongoDb> _logger;
	private readonly IMongoCollection<SenderSettings> _senderCollection;
	private Task _checkUpdates;

	public SenderSettingsFromMongoDb(ILogger<SenderSettingsFromMongoDb> logger, IMongoCollection<SenderSettings> senderCollection)
	{
		_logger = logger;
		_senderCollection = senderCollection;
		// _checkUpdates = new Task(() =>
		// {
		// 	var options = new ChangeStreamOptions {FullDocument = ChangeStreamFullDocumentOption.UpdateLookup};
		// 	var pipeline =
		// 		new EmptyPipelineDefinition<ChangeStreamDocument<SenderSettings>>().Match(
		// 			"{ operationType: { $in: [ 'insert', 'delete' ] } }");
		//
		// 	var cursor = _senderCollection.Watch(pipeline, options);
		//
		// 	var enumerator = cursor.ToEnumerable().GetEnumerator();
		// 	while (enumerator.MoveNext())
		// 	{
		// 		ChangeStreamDocument<SenderSettings> doc = enumerator.Current;
		// 		// Do something here with your document
		// 		Console.WriteLine(doc.DocumentKey);
		// 	}
		// });
	}

	public async IAsyncEnumerable<SenderSettings> Fetch()
	{
		await foreach (var settings in (await _senderCollection.FindAsync(FilterDefinition<SenderSettings>.Empty)).ToEnumerable().ToAsyncEnumerable())
			yield return settings;
	}

	public event Action OnUpdate;
}