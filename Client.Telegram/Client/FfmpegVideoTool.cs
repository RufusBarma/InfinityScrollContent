using Xabe.FFmpeg;

namespace Client.Telegram.Client;

public class FfmpegVideoTool : IVideoTool
{
	public async Task<VideoMetadata> GetMetadataAsync(string videoPath)
	{
		var metadata = await FFmpeg.GetMediaInfo(videoPath);
		var firstCapture = metadata.VideoStreams.FirstOrDefault();
		var hasAudio = metadata.AudioStreams.FirstOrDefault() != null;
		return new VideoMetadata
		{
			Duration = metadata.Duration,
			Height = firstCapture.Height,
			Width = firstCapture.Width,
			HasAudio = hasAudio
		};
	}

	public async Task<Stream> GetThumbnailAsync(string videoPath, TimeSpan seekSpan)
	{
		var outputPath = Path.ChangeExtension(videoPath, ".png");
		var conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(videoPath, outputPath, seekSpan);
		var result = await conversion.Start();
		var fileStream = File.OpenRead(outputPath);
		var outputStream = new MemoryStream();
		await fileStream.CopyToAsync(outputStream);
		outputStream.Position = 0;
		await fileStream.DisposeAsync();
		File.Delete(outputPath);
		return outputStream;
	}
}