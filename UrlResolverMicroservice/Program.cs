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
	.AddStackExchangeRedisCache(options =>
	{
		options.Configuration = "localhost:6379";
	})
	.AddSingleton<IMainResolver, MainResolver>()
	.AddSingleton<IUrlResolver, ImgurResolver>()
	.AddSingleton<IUrlResolver, RedditGalleryResolver>()
	.AddSingleton<IUrlResolver, RedGifsResolver>()
	.AddSingleton<RedGifsResolver>()
	.AddSingleton<IUrlResolver, GfycatResolver>()
	.AddSingleton<IConfiguration>(configurationRoot)
	.AddSingleton<IMongoDatabase>(_ =>
	{
		var mongoUrl = new MongoUrl(configurationRoot.GetConnectionString("MongoConnection") ?? configurationRoot.GetConnectionString("DefaultConnection"));
		return new MongoClient(mongoUrl.Url.Replace(mongoUrl.DatabaseName, "")).GetDatabase(mongoUrl.DatabaseName);
	})
	.AddLogging(configure => configure.AddConsole())
	.BuildServiceProvider();

var resolverService = serviceProvider.GetService<IMainResolver>();
var logger = serviceProvider.GetService<ILogger<Program>>();
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
	logger.LogInformation("Start resolving");
	currentTask = resolverService.Start(cancellationTokenSource.Token);
	await currentTask;
	if (cancellationTokenSource.IsCancellationRequested)
		break;
	logger.LogInformation("Complete resolving. Start waiting");
	currentTask = Task.Delay(new TimeSpan(0, 30, 0), cancellationTokenSource.Token);
	await currentTask;
}