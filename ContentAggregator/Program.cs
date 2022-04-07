using ContentAggregator.Aggregators;
using ContentAggregator.Aggregators.Reddit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configurationRoot = new ConfigurationBuilder()
	.AddEnvironmentVariables()
	.AddJsonFile("appsettings.json")
	.Build();

var serviceProvider = new ServiceCollection()
	.AddTransient<IAggregator, RedditAggregator>()
	.AddSingleton<AggregatorComposer>()
	.AddSingleton<RedditCategoriesAggregator>()
	.AddSingleton<IConfiguration>(configurationRoot)
	.AddLogging(configure => configure.AddConsole())
	.BuildServiceProvider();

var composer = serviceProvider.GetRequiredService<AggregatorComposer>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

var cancellationTokenSource = new CancellationTokenSource();
var currentTask = Task.CompletedTask;

AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
	logger.LogInformation("Received SIGTERM");
	cancellationTokenSource.Cancel();
	await currentTask.WaitAsync(CancellationToken.None);
	logger.LogInformation("Safety shutdown");
	await serviceProvider.DisposeAsync();
};

while (!cancellationTokenSource.IsCancellationRequested)
{
	logger.LogInformation("Start aggregation loop");
	currentTask = composer.Start(cancellationTokenSource.Token);
	await currentTask;
}