using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using static CsvParser;
using static DatabaseRepository;
using static FileMonitorService;
using static FileService;
using static ProcessingService;

public class Program
{
	public static Settings settings; // Impostazioni globali

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
			Log.Error(ex, "CsvProcessorApp terminated unexpectedly." + ex.Message);
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

			// **💡 Carichiamo AppConfig PRIMA di qualsiasi altra cosa**
			var appConfig = AppConfig.LoadConfig();
			services.AddSingleton(appConfig);  // 🔹 Ora AppConfig è registrato correttamente

			// **💡 Registriamo Settings e DatabaseContext DOPO AppConfig**
			var settings = Settings.LoadSettings();
			services.AddSingleton(settings);
			services.AddSingleton<IDatabaseContext, DatabaseContext>();

			services.AddSingleton<IDatabaseRepository, DatabaseRepository>();

			// **💡 Registriamo FileMonitorService DOPO AppConfig**
			services.AddSingleton<IFileMonitorService, FileMonitorService>();

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
