namespace Client.Telegram.Client;

public interface IVideoTool
{
	public Task<VideoMetadata> GetMetadataAsync(string videoPath);
	public Task<Stream> GetThumbnailAsync(string videoPath, TimeSpan seekSpan);
}

public record VideoMetadata
{
	public int Height { get; init; }
	public int Width { get; init; }
	public TimeSpan Duration { get; init; }
	public bool HasAudio { get; init; }
}