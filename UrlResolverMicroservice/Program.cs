using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using UrlResolverMicroservice.ResolveService;
using UrlResolverMicroservice.UrlResolvers;

var configurationRoot = new ConfigurationBuilder()
	.AddEnvironmentVariables()
	.AddJsonFile("appsettings.json")
	.Build();

var serviceProvider = new ServiceCollection()
	.AddSingleton<IMainResolver, MainResolver>()
	.AddTransient<IUrlResolver, RedGifsResolver>()
	.AddTransient<IUrlResolver, GfycatResolver>()
	.AddSingleton<IConfiguration>(configurationRoot)
	.AddSingleton<IMongoClient, MongoClient>(sp => new MongoClient(configurationRoot.GetConnectionString("DefaultConnection")))
	.AddLogging(configure => configure.AddConsole())
	.BuildServiceProvider();

var resolverService = serviceProvider.GetService<IMainResolver>();
var logger = serviceProvider.GetService<ILogger<Program>>();
var cancellationTokenSource = new CancellationTokenSource();
var mainTask = Task.CompletedTask;

AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
	cancellationTokenSource.Cancel();
	await mainTask.WaitAsync(cancellationTokenSource.Token);
	logger.LogInformation("Received SIGTERM");
	serviceProvider.Dispose();
};

while (true)
{
	logger.LogInformation("Start resolving");
	mainTask = resolverService.Start(cancellationTokenSource.Token);
	await mainTask;
	logger.LogInformation("Complete resolving. Start waiting");
	await Task.Delay(new TimeSpan(0, 30, 0), cancellationTokenSource.Token);
}