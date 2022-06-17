using System.Runtime.InteropServices;
using Client.Telegram.Boilerplates;
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
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

var configurationRoot = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddEnvironmentVariables()
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
		var mongoUrl = new MongoUrl(configurationRoot.GetConnectionString("MongoConnection") ?? configurationRoot.GetConnectionString("DefaultConnection"));
		return new MongoClient(mongoUrl.Url.Replace(mongoUrl.DatabaseName, "")).GetDatabase(mongoUrl.DatabaseName);
	})
	.AddTransient<IMongoCollection<SavedState>>(provider => provider.GetService<IMongoDatabase>().GetCollection<SavedState>("AccessHash"))
	.AddTransient<IMongoCollection<SenderSettings>>(provider => provider.GetService<IMongoDatabase>().GetCollection<SenderSettings>("SenderSettings"))
	.AddTransient<ISenderSettingsFetcher, SenderSettingsFromMongoDb>()
	.AddTransient<SendJobFactory>()
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
	.BuildServiceProvider();
GlobalConfiguration.Configuration.UseActivator(new ContainerJobActivator(serviceProvider));
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