using System.Drawing;
using System.Drawing.Drawing2D;
using TL;
using WTelegram;

namespace Client.Telegram.Client;

public static class TelegramClientExtensions
{
	/// <summary>Helper function to upload a file to Telegram</summary>
	/// <param name="pathname">Path to the file to upload</param>
	/// <param name="progress">(optional) Callback for tracking the progression of the transfer</param>
	/// <returns>an <see cref="InputFile" /> or <see cref="InputFileBig" /> than can be used in various requests</returns>
	public static async Task<InputFileBase> UploadFileAsync(this WTelegram.Client client, string pathname,
		WTelegram.Client.ProgressCallback progress = null, bool withCompression = false)
	{
		var imageExtensions = new[] {".jpeg", ".png", ".jpg", ".tiff"};
		var extension = Path.GetExtension(pathname).ToLower();
		if (string.IsNullOrEmpty(extension) || !withCompression || !imageExtensions.Contains(extension))
			return await client.UploadFileAsync(pathname, progress);
		var compressedPhoto = Resize(pathname);
		compressedPhoto.Position = 0;
		return await client.UploadFileAsync(compressedPhoto, Path.GetFileName(pathname), progress);
	}

	private static Stream Resize(string path)
	{
		return Resize(File.OpenRead(path));
	}

	private static Stream Resize(Stream stream)
	{
		var imgToResize = Image.FromStream(stream);
		var size = new Size(1280, 1280);
		//Get the image current width
		var sourceWidth = imgToResize.Width;
		//Get the image current height
		var sourceHeight = imgToResize.Height;
		float nPercent = 0;
		float nPercentW = 0;
		float nPercentH = 0;
		//Calulate  width with new desired size
		nPercentW = size.Width / (float) sourceWidth;
		//Calculate height with new desired size
		nPercentH = size.Height / (float) sourceHeight;
		if (nPercentH < nPercentW)
			nPercent = nPercentH;
		else
			nPercent = nPercentW;
		//New Width
		var destWidth = (int) (sourceWidth * nPercent);
		//New Height
		var destHeight = (int) (sourceHeight * nPercent);
		var b = new Bitmap(destWidth, destHeight);
		var g = Graphics.FromImage(b);
		g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		// Draw image with new width and height
		g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
		g.Dispose();
		var output = new MemoryStream();
		b.Save(output, imgToResize.RawFormat);
		return output;
	}

	public static async Task<Message> SafeSendAlbumAsync(this WTelegram.Client client, InputPeer peer, InputMedia[] medias, string caption = null, int reply_to_msg_id = 0, MessageEntity[] entities = null, DateTime schedule_date = default)
	{
		var convertedMedias = new InputMedia[medias.Length];
		for (var i = 0; i < medias.Length; i++)
		{
			var ism = medias[i];
			convertedMedias[i] = ism switch
			{
				InputMediaPhotoExternal image => await GetPhoto(image, client),
				_ => ism
			};
		}
		return await client.SendAlbumAsync(peer, convertedMedias, caption, reply_to_msg_id, entities, schedule_date);
	}

	private static async Task<InputMedia> GetPhoto(InputMediaPhotoExternal impe, WTelegram.Client client)
	{
		var inputFile = await UploadFromUrl(impe.url);
		return new InputMediaUploadedPhoto { file = inputFile };

		async Task<InputFileBase> UploadFromUrl(string url)
		{
			var httpClient = new HttpClient();
			var filename = Path.GetFileName(new Uri(url).LocalPath);
			var response = await httpClient.GetAsync(url);
			await using var stream = await response.Content.ReadAsStreamAsync();
			if (response.Content.Headers.ContentLength is long length)
			{
				var content = new Helpers.IndirectStream(stream) {ContentLength = length};
				await using var resized = Resize(content);
				resized.Position = 0;
				return await client.UploadFileAsync(resized, filename);
			}
			else
			{
				await using var resized = Resize(stream);
				resized.Position = 0;
				return await client.UploadFileAsync(resized, filename);
			}
		}
	}
}