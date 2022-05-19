namespace Client.Telegram.SenderSettings;

public interface ISenderSettingsFetcher
{
	public IAsyncEnumerable<SenderSettings> Fetch();
	public event Func<bool> OnUpdate;
}