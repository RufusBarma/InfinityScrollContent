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
var cancellationTokenSource = new CancellationTokenSource();
var mainTask = resolverService.Start(cancellationTokenSource.Token);
await mainTask;
// Console.ReadLine();
// cancellationTokenSource.Cancel();
// await mainTask.WaitAsync(cancellationTokenSource.Token);
serviceProvider.Dispose();