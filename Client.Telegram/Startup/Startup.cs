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
	private readonly ISenderSettingsFetcher _fetcher;

	public Startup(ILogger<Startup> logger, ISenderSettingsFetcher fetcher, IBackgroundProcessingServer server, SendJobFactory sendJobFactory)
	{
		_logger = logger;
		_server = server;
		_sendJobFactory = sendJobFactory;
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
		await foreach (var settings in _fetcher.Fetch().Skip(1))
		{
			RecurringJob.AddOrUpdate(settings.ChannelUsername,
				() => _sendJobFactory.GetJobs(settings).Execute(CancellationToken.None), settings.Cron);

			// BackgroundJob.Enqueue(() => _sendJobFactory.GetJobs(settings).Execute(CancellationToken.None));
			break;
		}
	}
}