namespace Client.Telegram.SenderSettings;

public interface ISenderSettingsFetcher
{
	public IAsyncEnumerable<SenderSettings> Fetch();
	public event Action OnUpdate;
}