namespace Client.Telegram.Startup;

public interface IStartup
{
	public Task Start(CancellationToken cancellationToken);
}