using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Client.Telegram.Client;

public class SendJobFactory
{
	private readonly IServiceProvider _serviceProvider;

	public SendJobFactory(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	public SendJob GetJobs(SenderSettings.SenderSettings settings)
	{
		return ActivatorUtilities.CreateInstance<SendJob>(_serviceProvider, settings);
	}

	[AutomaticRetry(Attempts = 3, DelaysInSeconds = new []{60, 60*30, 60*60})]
	[DisableConcurrentExecution(60*30)] //30 minutes
	public async Task ExecuteJob(SenderSettings.SenderSettings settings, CancellationToken cancellationToken)
	{
		await GetJobs(settings).Execute(cancellationToken);
	}
}