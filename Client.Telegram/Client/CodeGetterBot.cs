using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Client.Telegram.Client;

public class CodeGetterBot
{
	private readonly ILogger _logger;
	private readonly TelegramBotClient _botClient;
	private readonly CancellationTokenSource _cancellationTokenSource = new ();
	private string lastMessage = "";
	private readonly string _chatId;

	public CodeGetterBot(IConfiguration config, ILogger<CodeGetterBot> logger)
	{
		_logger = logger;
		var token = config["telegram.bot.token"];
		_chatId = config["admin.chat.id"];
		_botClient = new TelegramBotClient(token);
		// TODO cancel receiving after gotten
		_botClient.StartReceiving(
			HandleUpdateAsync,
			HandleErrorAsync,
			new ReceiverOptions(),
			_cancellationTokenSource.Token);
	}

	public string GetCode()
	{
		_logger.LogInformation("Await auth code");
		return GetCodeAsync().Result;
	}

	private async Task<string> GetCodeAsync()
	{
		await _botClient.SendTextMessageAsync(_chatId, "Please, write auth code:");
		while (string.IsNullOrEmpty(lastMessage))
			await Task.Delay(TimeSpan.FromSeconds(10));
		_logger.LogInformation("Auth code gotten");
		_cancellationTokenSource.Cancel();
		return lastMessage;
	}

	private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
		CancellationToken cancellationToken)
	{
		// Only process Message updates: https://core.telegram.org/bots/api#message
		if (update.Type != UpdateType.Message)
			return;
		// Only process text messages
		if (update.Message!.Type != MessageType.Text)
			return;

		var chatId = update.Message.Chat.Id;
		var messageText = update.Message.Text;
		_logger.LogDebug("Received a '{messageText}' message in chat {chatId}.", messageText, chatId);
		if (chatId.ToString() == _chatId && int.TryParse(messageText, out _))
			lastMessage = messageText;
	}

	private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
		CancellationToken cancellationToken)
	{
		var ErrorMessage = exception switch
		{
			ApiRequestException apiRequestException
				=> $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
			_ => exception.ToString()
		};

		_logger.LogError(ErrorMessage);
	}
}