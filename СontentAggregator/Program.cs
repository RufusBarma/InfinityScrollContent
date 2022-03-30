using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using СontentAggregator.Aggregators;
using СontentAggregator.Aggregators.Reddit;

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

var composer = serviceProvider.GetService<AggregatorComposer>();
composer.Start();
serviceProvider.Dispose();