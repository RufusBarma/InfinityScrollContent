namespace Bot.Telegram.Models;

public record Channel
{
	public string Name { get; }
	public string[] Categories { get; }
	public string[] SubReddits { get; }
	public string CronShedule { get; }
}