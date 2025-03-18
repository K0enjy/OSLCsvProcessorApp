using BRUNINI_CsvParser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

public class Program
{
	public static Settings settings;

	static void Main(string[] args)
	{
		Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

		try
		{
			Log.Information("Starting CsvProcessorApp");

			CreateHostBuilder(args)
				.Build()
				.Run();
		}
		catch (Exception ex)
		{
			Log.Error($"CsvProcessorApp terminated unexpectedly: {ex.Message}" );
		}
		finally
		{
			Log.CloseAndFlush();
		}

		Log.Information("CsvProcessorApp Closed");
	}

	public static IHostBuilder CreateHostBuilder(string[] args) =>
	Host.CreateDefaultBuilder(args)
		.UseWindowsService()
		.UseSerilog()
		.ConfigureServices((context, services) =>
		{
			IConfiguration config = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(config)
				.CreateLogger();

			var settings = Settings.LoadConfig();
			services.AddSingleton(settings);
			services.AddSingleton<IDatabaseContext, DatabaseContext>();

			services.AddSingleton<IDatabaseOperations, DatabaseOperations>();
			services.AddSingleton<IFileMonitor, FileMonitor>();
			services.AddSingleton<IProcessingService, ProcessingService>();
			services.AddSingleton<IFileService, FileService>();
			services.AddSingleton<ICsvParser, CsvParser>();

			services.AddLogging(loggingBuilder =>
			{
				loggingBuilder.ClearProviders();
				loggingBuilder.AddSerilog();
			});
		});


}
