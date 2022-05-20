using System.Runtime.InteropServices;
using Client.Telegram.Client;
using Client.Telegram.SenderSettings;
using Client.Telegram.Startup;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Mono.Unix.Native;
using MoreLinq;
using Quartz;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

var configurationRoot = new ConfigurationBuilder()
	.AddEnvironmentVariables()
	.AddJsonFile("appsettings.json")
	.Build();

var ffmpegPath = "./FFmpeg";
FFmpeg.SetExecutablesPath(ffmpegPath);
await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
	new DirectoryInfo(ffmpegPath)
		.GetFiles()
		.Where(file => !file.Name.EndsWith(".json"))
		.ForEach(file => Syscall.chmod(file.FullName, FilePermissions.S_IRUSR | FilePermissions.S_IWUSR | FilePermissions.S_IXUSR));
}

var serviceProvider = new ServiceCollection()
	.AddTransient<IVideoTool, FfmpegVideoTool>()
	.AddSingleton(_ =>
		new WTelegram.Client(what =>
		{
			if (what != "verification_code") return configurationRoot.GetValue<string>(what);
			Console.Write("Code: ");
			return Console.ReadLine();
		})
			{CollectAccessHash = true}
	)
	.AddSingleton<IConfiguration>(configurationRoot)
	.AddLogging(configure => configure.AddConsole())
	.AddTransient<SendJob>()
	.AddSingleton<IMongoDatabase>(_ =>
	{
		var mongoUrl = new MongoUrl(configurationRoot.GetConnectionString("DefaultConnection"));
		return new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName);
	})
	.AddTransient<IMongoCollection<SavedState>>(provider => provider.GetService<IMongoDatabase>().GetCollection<SavedState>("AccessHash"))
	.AddTransient<IMongoCollection<SenderSettings>>(provider => provider.GetService<IMongoDatabase>().GetCollection<SenderSettings>("SenderSettings"))
	.AddTransient<ISenderSettingsFetcher, SenderSettingsFromMongoDb>()
	.AddTransient<SendJobFactory>()
	// .AddTransient<SenderSettings>(provider => provider.GetRequiredService<ISenderSettingsFetcher>().Fetch().ToEnumerable().First())
	.AddTransient<ISender, ClientSender>()
	.AddSingleton<ClientSenderExtensions>()
	.AddTransient<IStartup, Startup>()
	.AddHangfire(configuration => configuration
		.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
		.UseSimpleAssemblyNameTypeSerializer()
		.UseRecommendedSerializerSettings()
		.UseMemoryStorage()
	)
	.AddSingleton<IBackgroundProcessingServer, BackgroundJobServer>()
	.AddHangfireServer()
	// .AddQuartz(q =>
	// 	{
	// 		// handy when part of cluster or you want to otherwise identify multiple schedulers
	// 		q.SchedulerId = "Scheduler-Core";
	// 		// we take this from appsettings.json, just show it's possible
	// 		q.SchedulerName = "Quartz ASP.NET Core Sample Scheduler";
	//
	// 		q.UseMicrosoftDependencyInjectionJobFactory();
	//
	// 		// these are the defaults
	// 		q.UseSimpleTypeLoader();
	// 		q.UseInMemoryStore();
	// 		q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 10; });
	//
	// 		q.ScheduleJob<SendJob>(trigger => trigger
	// 			.WithIdentity("Combined Configuration Trigger")
	// 			.StartNow()
	// 			// .WithCronSchedule("0 0 */2 * * ?")
	// 			.WithDescription("my awesome trigger configured for a job with single call")
	// 		);
	// 	})
	// .AddQuartzHostedService(q => q.WaitForJobsToComplete = true)
	.BuildServiceProvider();

var telegramLogger = serviceProvider.GetRequiredService<ILogger<WTelegram.Client>>();
WTelegram.Helpers.Log = (lvl, str) => telegramLogger.Log((LogLevel)lvl, str);

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var startup = serviceProvider.GetRequiredService<IStartup>();
var cancellationTokenSource = new CancellationTokenSource();
var startupTask = startup.Start(cancellationTokenSource.Token);

AppDomain.CurrentDomain.ProcessExit += async (_, _) =>
{
	logger.LogInformation("Received SIGTERM");
	cancellationTokenSource.Cancel();
	await startupTask;
	logger.LogInformation("Safety shutdown");
	await serviceProvider.DisposeAsync();
};

await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);