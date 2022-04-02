using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Telegram.Bot;

public class BotStartup
{
    private readonly ILogger<BotStartup> _logger;
    private TelegramBotClient _botClient;

    public BotStartup(IConfiguration config, ILogger<BotStartup> logger)
    {
        _logger = logger;
        var token = config["telegram.bot.token"];
        _botClient = new TelegramBotClient(token);
    }

    public async Task Start(CancellationToken ctsToken)
    {

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { } // receive all update types
        };
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            ctsToken);

        var me = await _botClient.GetMeAsync();
        _logger.LogInformation("Start listening for {Username}", me.Username);
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                var chatIdLocal = update.CallbackQuery.Message.Chat.Id;
                var messageId = update.CallbackQuery.Message.MessageId;
                
                var inlineKeyboard = new InlineKeyboardMarkup(new []
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "0.1", callbackData: "11"),
                        InlineKeyboardButton.WithCallbackData(text: "0.2", callbackData: "12"),
                    },
                });
                try
                {
                    var message = await botClient.EditMessageReplyMarkupAsync(chatIdLocal, messageId, inlineKeyboard);
                    await botClient.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Edited", replyMarkup: inlineKeyboard);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                return;
            }
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Type != UpdateType.Message)
                return;
            // Only process text messages
            if (update.Message!.Type != MessageType.Text)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;

            _logger.LogDebug("Received a '{messageText}' message in chat {chatId}.", messageText, chatId);

            
            var inlineKeyboardOrg = new InlineKeyboardMarkup(new []
            {
                // first row
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "0.1", callbackData: "11"),
                    InlineKeyboardButton.WithCallbackData(text: "0.2", callbackData: "12"),
                },
                // second row
                new []
                {
                    InlineKeyboardButton.WithCallbackData(text: "1.1", callbackData: "21"),
                    InlineKeyboardButton.WithCallbackData(text: "1.2", callbackData: "22"),
                },
            });
            
            // Echo received message text
            var sentMessage = await botClient.SendTextMessageAsync(
                chatId,
                "You said:\n" + messageText,
                replyMarkup: inlineKeyboardOrg,
                cancellationToken: cancellationToken);
        }

        private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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