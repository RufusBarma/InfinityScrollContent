using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
	.BuildServiceProvider();

var composer = serviceProvider.GetService<AggregatorComposer>();
composer.Start();
// var categories = serviceProvider.GetService<RedditCategoriesAggregator>().GetCategories();
Console.WriteLine("Finish");