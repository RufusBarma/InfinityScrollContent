using System.Text.Json;
using Client.Telegram.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Quartz;
using TL;

var configurationRoot = new ConfigurationBuilder()
	.AddEnvironmentVariables()
	.AddJsonFile("appsettings.json")
	.Build();

var serviceProvider = new ServiceCollection()
	.AddTransient(_ =>
	{
		var client = new WTelegram.Client(what =>
		{
			if (what != "verification_code") return configurationRoot.GetValue<string>(what);
			Console.Write("Code: ");
			return Console.ReadLine();
		});
		client.CollectAccessHash = true;			
		
		if (File.Exists("SavedState.json"))
		{
			//TODO save states in db
			Console.WriteLine("Loading previously saved access hashes from disk...");
			using var stateStream = File.OpenRead("SavedState.json");
			var savedState = JsonSerializer.Deserialize<SavedState>(stateStream);
			foreach (var id_hash in savedState.Channels) client.SetAccessHashFor<Channel>(id_hash.Key, id_hash.Value);
			foreach (var id_hash in savedState.Users) client.SetAccessHashFor<User>(id_hash.Key, id_hash.Value);
		}

		return client;
	})
	.AddSingleton<IConfiguration>(configurationRoot)
	.AddLogging(configure => configure.AddConsole())
	.AddTransient<SendJob>()
	.AddSingleton<IMongoClient, MongoClient>(sp =>
		new MongoClient(configurationRoot.GetConnectionString("DefaultConnection")))
	.AddQuartz(q =>
		{
			// handy when part of cluster or you want to otherwise identify multiple schedulers
			q.SchedulerId = "Scheduler-Core";
			// we take this from appsettings.json, just show it's possible
			q.SchedulerName = "Quartz ASP.NET Core Sample Scheduler";

			q.UseMicrosoftDependencyInjectionJobFactory();

			// these are the defaults
			q.UseSimpleTypeLoader();
			q.UseInMemoryStore();
			q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });

			q.ScheduleJob<SendJob>(trigger => trigger
				.WithIdentity("Combined Configuration Trigger")
				// .StartNow()
				.WithCronSchedule("0 0 */1 * * ?")
				.WithDescription("my awesome trigger configured for a job with single call")
			);
		})
	.AddQuartzHostedService(q => q.WaitForJobsToComplete = true)
	.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var scheduler = await serviceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler();
var cancellationTokenSource = new CancellationTokenSource();
await scheduler.Start(cancellationTokenSource.Token);

AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
	logger.LogInformation("Received SIGTERM");
	cancellationTokenSource.Cancel();
	await scheduler.Shutdown(true);
	logger.LogInformation("Safety shutdown");
	await serviceProvider.DisposeAsync();
};

await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);