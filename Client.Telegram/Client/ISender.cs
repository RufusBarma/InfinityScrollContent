using Client.Telegram.Models;

namespace Client.Telegram.Client;

public interface ISender
{
	public IAsyncEnumerable<Link> Send(IEnumerable<Link> links, long channelId, string channelUserName, CancellationToken cancellationToken);
}