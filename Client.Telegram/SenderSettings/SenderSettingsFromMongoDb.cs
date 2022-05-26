using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MoreLinq.Extensions;

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
		Task.Run(CheckUpdates);
	}

	private async Task CheckUpdates()
	{
		var options = new ChangeStreamOptions {FullDocument = ChangeStreamFullDocumentOption.UpdateLookup};
		var pipeline =
			new EmptyPipelineDefinition<ChangeStreamDocument<SenderSettings>>().Match(
				"{ operationType: { $in: [ 'insert', 'delete', 'update' ] } }");

		var cursor = _senderCollection.Watch(pipeline, options);

		cursor
			.ToEnumerable()
			.ForEach(doc =>
			{
				_logger.LogInformation("SenderSettings have been changed");
				if (doc.OperationType == ChangeStreamOperationType.Delete)
					OnDelete?.Invoke(doc.DocumentKey["_id"].ToString());
				else
					OnUpdate?.Invoke();
			});
	}

	public async IAsyncEnumerable<SenderSettings> Fetch()
	{
		await foreach (var settings in (await _senderCollection.FindAsync(FilterDefinition<SenderSettings>.Empty)).ToEnumerable().ToAsyncEnumerable())
			yield return settings;
	}

	public event Action OnUpdate;
	public event Action<string> OnDelete;
}