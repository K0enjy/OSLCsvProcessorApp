using Microsoft.Extensions.Configuration;

public class AppConfig
{
	public string ConnectionStringDatabase { get; set; }
	public string CsvFolder { get; set; }
	public string ArchiveFolder { get; set; }

	public static AppConfig LoadConfig()
	{
		IConfiguration config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false)
			.Build();

		return config.GetRequiredSection("Settings").Get<AppConfig>() ??
			   throw new Exception("Errore nel caricamento delle impostazioni da appsettings.json.");
	}
}
