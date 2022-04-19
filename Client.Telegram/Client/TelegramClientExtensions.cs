using Aspose.Imaging;
using Aspose.Imaging.ImageOptions;
using TL;

namespace Client.Telegram.Client;

public static class TelegramClientExtensions
{
	/// <summary>Helper function to upload a file to Telegram</summary>
	/// <param name="pathname">Path to the file to upload</param>
	/// <param name="progress">(optional) Callback for tracking the progression of the transfer</param>
	/// <returns>an <see cref="InputFile"/> or <see cref="InputFileBig"/> than can be used in various requests</returns>
	public static async Task<InputFileBase> UploadFileAsync(this WTelegram.Client client, string pathname,
		WTelegram.Client.ProgressCallback progress = null, bool withCompression = false)
	{
		var imageExtensions = new[] {".jpeg", ".png", ".jpg", ".tiff"};
		var extension = Path.GetExtension(pathname).ToLower();
		if (string.IsNullOrEmpty(extension) || !withCompression || !imageExtensions.Contains(extension))
			return await client.UploadFileAsync(pathname, progress);
		var compressedFilePath = Resize(pathname);
		return await client.UploadFileAsync(File.OpenRead(compressedFilePath), Path.GetFileName(pathname), progress);
	}

	private static string Resize(string pathname)
	{
		using var image = Image.Load(pathname);
		var maxSize = Math.Max(image.Height, image.Width);
		if (maxSize <= 1280)
			return pathname;
		if (image.Height > image.Width)
			image.ResizeHeightProportionally(1280);
		else 
			image.ResizeWidthProportionally(1280);
		image.Save(pathname, true);
		return pathname;
	}
}