using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using TL;
using WTelegram;

namespace Client.Telegram.Client;

public static class TelegramClientExtensions
{
	private const int TenMegaBytes = 10 * 1024 * 1024;

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
		var compressedPhoto = await Resize(pathname);
		return await client.UploadFileAsync(compressedPhoto, Path.GetFileName(pathname), progress);
	}

	private static async Task<Stream> Resize(string path)
	{
		return await Resize(File.OpenRead(path));
	}

	private static async Task<Stream> Resize(Stream stream)
	{
		(Image Image, IImageFormat Format) imf = await Image.LoadWithFormatAsync(stream);
		using var image = imf.Image;
		var maxSize = Math.Max(image.Width, image.Height);
		if (maxSize <= 1280)
		{
			stream.Position = 0;
			return stream;
		}

		image.Mutate(x => x.Resize(new ResizeOptions {Mode = ResizeMode.Max, Size = new Size(1280, 1280)}));
		var output = new MemoryStream();
		await image.SaveAsync(output, imf.Format); 
		output.Position = 0;
		return output;
	}

	public static async Task<List<Message>> SafeSendAlbumAsync(this WTelegram.Client client, InputPeer peer, InputMedia[] medias, string caption = null, int reply_to_msg_id = 0, MessageEntity[] entities = null, DateTime schedule_date = default)
	{
		var convertedMedias = new InputMedia[medias.Length];
		for (var i = 0; i < medias.Length; i++)
		{
			var ism = medias[i];
			convertedMedias[i] = ism switch
			{
				InputMediaPhotoExternal image => await GetPhoto(image, client),
				InputMediaDocumentExternal document => await GetDocument(document, client),
				_ => ism
			};
		}

		var mediaToSendAsMessage = convertedMedias
			.Where(media =>
			{
				var uploaded = (media as InputMediaUploadedDocument);
				var isGif = uploaded != null && 
				            (uploaded.attributes != null && uploaded.attributes.Any(attribute => attribute.GetType() == typeof(DocumentAttributeAnimated)) || 
				             uploaded.file != null && uploaded.file.Name.EndsWith("gif"));
				return media.GetType() == typeof(InputMediaDocumentExternal) || isGif;
			})
			.ToList();
		var messages = new List<Message>();
		foreach (var media in mediaToSendAsMessage)
			messages.Add(await client.SendMessageAsync(peer, caption, media, reply_to_msg_id, entities, schedule_date));
		var album = convertedMedias.Except(mediaToSendAsMessage).ToArray();
		if (album.Any())
			messages.Add(await client.SendAlbumAsync(peer, album, caption, reply_to_msg_id, entities, schedule_date));
		return messages;
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
				await using var resized = await Resize(content);
				return await client.UploadFileAsync(resized, filename);
			}
			else
			{
				await using var resized = await Resize(stream);
				return await client.UploadFileAsync(resized, filename);
			}
		}
	}

	private static async Task<InputMedia> GetDocument(InputMediaDocumentExternal document, WTelegram.Client client)
	{
		//TODO refactoring need
		var inputFile = await UploadFromUrl(document.url);
		return inputFile;

		async Task<InputMedia> UploadFromUrl(string url)
		{
			var httpClient = new HttpClient();
			httpClient.Timeout = new TimeSpan(0, 30, 0);
			var filename = Path.GetFileName(new Uri(url).LocalPath);
			var response = await httpClient.GetAsync(url);
			using var stream = await response.Content.ReadAsStreamAsync();
			var mimeType = response.Content.Headers.ContentType?.MediaType;
			if (mimeType != "video/mp4")
			{
				return document;
			}
			else
			{
				if (!Directory.Exists("tmp"))
					Directory.CreateDirectory("tmp");
				var filePath = Path.Combine("tmp", filename);
				var fileInfo = new FileInfo(filePath);
				using (var fileStream = fileInfo.OpenWrite())
				{
					await stream.CopyToAsync(fileStream);
					if (response.Content.Headers.ContentLength is long length)
					{
						var indirect = new Helpers.IndirectStream(stream) {ContentLength = length};
						await indirect.CopyToAsync(fileStream);
					}
					else
						await stream.CopyToAsync(fileStream);
				}
				var mediaFile = new MediaFile { Filename = filePath };
				var thumbNailFile = new MediaFile(Path.ChangeExtension(filePath, "jpg"));
				using (var engine = new Engine())
				{
					engine.GetMetadata(mediaFile);
					engine.GetThumbnail(mediaFile, thumbNailFile, new ConversionOptions {Seek = mediaFile.Metadata.Duration.Divide(3)});
				}

				var thumbNailResized = await Resize(thumbNailFile.Filename);
				var thumbNailUploaded = await client.UploadFileAsync(thumbNailResized, thumbNailFile.Filename);

				var duration = mediaFile.Metadata.Duration.TotalSeconds;
				var size = mediaFile.Metadata.VideoData.FrameSize?.Split('x');
				var inputFileClient = await client.UploadFileAsync(filePath);
				var withAudio = mediaFile.Metadata.AudioData != null;
				var isSmallFile = fileInfo.Length < TenMegaBytes;
				var isGif = isSmallFile && !withAudio;
				var inputMedia = new InputMediaUploadedDocument
				{
					thumb = thumbNailUploaded,
					flags = InputMediaUploadedDocument.Flags.has_thumb,
					file = inputFileClient, 
					mime_type = isGif? "image/gif": "video/mp4",
					attributes = isGif? new[] {new DocumentAttributeAnimated()}: 
						new[] 
						{
							new DocumentAttributeVideo
							{
								duration = (int)duration,
								w = int.Parse(size[0]),
								h = int.Parse(size[1]),
								flags = DocumentAttributeVideo.Flags.supports_streaming
							}
						}
				};
				ClearCache();
				return inputMedia;
			}
		}
	}

	private static void ClearCache()
	{
		if (Directory.Exists("tmp"))
			Directory.Delete("tmp", true);
	}
}