using Client.Telegram.Client;
using Client.Telegram.SenderSettings;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace Client.Telegram.Startup;

public class Startup: IStartup
{
	private readonly ILogger<Startup> _logger;
	private readonly IBackgroundProcessingServer _server;
	private readonly SendJobFactory _sendJobFactory;
	private readonly IRecurringJobManager _jobManager;
	private readonly ISenderSettingsFetcher _fetcher;

	public Startup(ILogger<Startup> logger, ISenderSettingsFetcher fetcher, IBackgroundProcessingServer server, SendJobFactory sendJobFactory, IRecurringJobManager jobManager)
	{
		_logger = logger;
		_server = server;
		_sendJobFactory = sendJobFactory;
		_jobManager = jobManager;
		_fetcher = fetcher;
	}

	public async Task Start(CancellationToken cancellationToken)
	{
		await PlanSenders();
		_fetcher.OnUpdate += async () =>
		{
			if (!cancellationToken.IsCancellationRequested)
				await PlanSenders();
		};
		await Task.FromCanceled(cancellationToken);
		_server.SendStop();
		await _server.WaitForShutdownAsync(CancellationToken.None);
	}

	private async Task PlanSenders()
	{
		_logger.LogInformation("Plan senders");
		await foreach (var settings in _fetcher.Fetch())
		{
			_jobManager.AddOrUpdate(
				settings.ChannelUsername,
				() => _sendJobFactory.ExecuteJob(settings, CancellationToken.None),
				settings.Cron);
		}
	}
}