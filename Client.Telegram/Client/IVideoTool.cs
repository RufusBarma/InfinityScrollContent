using MediaToolkit.Services;
using MediaToolkit.Tasks;

namespace Client.Telegram.Client;

public interface IVideoTool
{
	public Task<VideoMetadata> GetMetadataAsync(string videoPath);
	public Task<Stream> GetThumbnailAsync(string videoPath, TimeSpan seekSpan);
}

public class FfmpegVideoTool : IVideoTool
{
	private readonly IMediaToolkitService _mediaToolkit;

	public FfmpegVideoTool(IMediaToolkitService mediaToolkit)
	{
		_mediaToolkit = mediaToolkit;
	}

	public async Task<VideoMetadata> GetMetadataAsync(string videoPath)
	{
		var metadataTask = new FfTaskGetMetadata(videoPath);
		var metadataResult = await _mediaToolkit.ExecuteAsync(metadataTask);
		var format = metadataResult.Metadata.Format;
		var size = format.Size.Split('x');
		var bitRate = format.BitRate;
		return new VideoMetadata()
		{
			Duration = TimeSpan.Parse(format.Duration),
			FrameSize = (int.Parse(size[0]), int.Parse(size[1])),
			HasAudio = bitRate != null
		};
	}

	public async Task<Stream> GetThumbnailAsync(string videoPath, TimeSpan seekSpan)
	{
		var thumbnailTask = new FfTaskGetThumbnail(videoPath, new GetThumbnailOptions()
		{
			SeekSpan = seekSpan
		});
		var thumbnailResult = await _mediaToolkit.ExecuteAsync(thumbnailTask);
		return new MemoryStream(thumbnailResult.ThumbnailData);
	}
}

public record VideoMetadata
{
	public (int height, int width) FrameSize { get; init; }
	public TimeSpan Duration { get; init; }
	public bool HasAudio { get; init; }
}