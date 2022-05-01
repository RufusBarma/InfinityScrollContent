namespace Client.Telegram.Client;

public class SavedState
{
    public List<KeyValuePair<long, long>> Channels { get; set; } = new();
    public List<KeyValuePair<long, long>> Users { get; set; } = new();
}