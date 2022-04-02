using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var configurationRoot = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configurationRoot)
    .AddLogging(configure => configure.AddConsole())
    .BuildServiceProvider();

var token = serviceProvider.GetService<IConfiguration>()?["telegram.bot.token"];

var botClient = new TelegramBotClient(token);
using var cts = new CancellationTokenSource();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { } // receive all update types
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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
                InlineKeyboardButton.WithCallbackData(text: "1.1", callbackData: "11"),
                InlineKeyboardButton.WithCallbackData(text: "1.2", callbackData: "12"),
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

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    
    InlineKeyboardMarkup inlineKeyboardOrg = new(new []
    {
        // first row
        new []
        {
            InlineKeyboardButton.WithCallbackData(text: "1.1", callbackData: "11"),
            InlineKeyboardButton.WithCallbackData(text: "1.2", callbackData: "12"),
        },
        // second row
        new []
        {
            InlineKeyboardButton.WithCallbackData(text: "2.1", callbackData: "21"),
            InlineKeyboardButton.WithCallbackData(text: "2.2", callbackData: "22"),
        },
    });
    
    // Echo received message text
    Message sentMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "You said:\n" + messageText,
        replyMarkup: inlineKeyboardOrg,
        cancellationToken: cancellationToken);
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}