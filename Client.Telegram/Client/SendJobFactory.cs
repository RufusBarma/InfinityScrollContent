using Client.Telegram.SenderSettings;
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

	public async Task ExecuteJob(SenderSettings.SenderSettings settings, CancellationToken cancellationToken)
	{
		await GetJobs(settings).Execute(cancellationToken);
	}
}