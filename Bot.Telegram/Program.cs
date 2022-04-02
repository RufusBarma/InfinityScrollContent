using Bot.Telegram.Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configurationRoot = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configurationRoot)
    .AddTransient<BotStartup>()
    .AddLogging(configure => configure.AddConsole())
    .BuildServiceProvider();

var cts = new CancellationTokenSource();
var startup = serviceProvider.GetService<BotStartup>();
startup.Start(cts.Token);
Console.ReadLine();
cts.Cancel();