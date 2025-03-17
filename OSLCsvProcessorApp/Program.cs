using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using static CsvParser;
using static DatabaseService;
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
			Log.Fatal(ex, "CsvProcessorApp terminated unexpectedly.");
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
			.UseSerilog() // Integra Serilog come provider di logging
			.ConfigureServices((context, services) =>
			{
				// Carichiamo la configurazione da appsettings.json
				IConfiguration config = new ConfigurationBuilder()
					.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
					.Build();

				// Inizializziamo Serilog leggendo da appsettings.json
				Log.Logger = new LoggerConfiguration()
					.ReadFrom.Configuration(config)
					.CreateLogger();

				// Carichiamo le impostazioni globali
				settings = Settings.LoadSettings();

				services.AddTransient<IDatabaseContext, DatabaseContext>();
				services.AddTransient<IDatabaseService, DatabaseService>();
				services.AddTransient<IFileMonitorService, FileMonitorService>();
				services.AddTransient<IProcessingService, ProcessingService>();
				services.AddTransient<IFileService, FileService>();
				services.AddTransient<ICsvParser, CsvParser>();

				// Aggiungiamo il servizio che esegue il monitoraggio in background
				services.AddHostedService<MonitorBackgroundService>();
			});
}
