using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UrlResolverMicroservice.ResolveTask;
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
	.AddLogging(configure => configure.AddConsole())
	.BuildServiceProvider();

var composer = serviceProvider.GetService<MainResolver>();
var cancellationTokenSource = new CancellationTokenSource();
var mainTask = composer.Start(cancellationTokenSource.Token);
Console.ReadLine();
cancellationTokenSource.Cancel();
await mainTask.WaitAsync(cancellationTokenSource.Token);
serviceProvider.Dispose();