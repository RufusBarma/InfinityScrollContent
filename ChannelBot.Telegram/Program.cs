using Bot.Telegram.Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Telegram.Bot;

var configurationRoot = new ConfigurationBuilder()
	.AddEnvironmentVariables()
	.AddJsonFile("appsettings.json")
	.Build();

var serviceProvider = new ServiceCollection()
	.AddSingleton<ITelegramBotClient>(sp =>
	{
		var token = configurationRoot["telegram.bot.token"];
		return new TelegramBotClient(token);
	})
	.AddSingleton<IConfiguration>(configurationRoot)
	.AddSingleton<BotStartup>()
	.AddLogging(configure => configure.AddConsole())
	.AddSingleton<IMongoClient, MongoClient>(sp =>
		new MongoClient(configurationRoot.GetConnectionString("DefaultConnection")))
	.AddSingleton<MailingQueue>()
	.BuildServiceProvider();

var startup = serviceProvider.GetRequiredService<MailingQueue>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var cancellationTokenSource = new CancellationTokenSource();
var currentTask = startup.Start(cancellationTokenSource.Token);

AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
	logger.LogInformation("Received SIGTERM");
	cancellationTokenSource.Cancel();
	await currentTask.WaitAsync(CancellationToken.None);
	logger.LogInformation("Safety shutdown");
	await serviceProvider.DisposeAsync();
};

await currentTask;