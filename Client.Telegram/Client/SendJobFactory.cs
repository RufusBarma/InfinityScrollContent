using Client.Telegram.SenderSettings;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Telegram.Client;

public class SendJobFactory
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ISenderSettingsFetcher _fetcher;

	public SendJobFactory(IServiceProvider serviceProvider, ISenderSettingsFetcher fetcher)
	{
		_serviceProvider = serviceProvider;
		_fetcher = fetcher;
	}

	public async IAsyncEnumerable<SendJob> GetJobs()
	{
		await foreach (var settings in _fetcher.Fetch())
			yield return ActivatorUtilities.CreateInstance<SendJob>(_serviceProvider, settings);
	}
}