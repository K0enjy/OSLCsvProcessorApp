using Microsoft.Extensions.Configuration;
using System;

public class Settings
{
	public string ConnectionStringDatabase { get; set; }
	public string CsvFolder { get; set; }
	public string ArchiveFolder { get; set; }

	public static Settings LoadConfig()
	{
		IConfiguration config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false)
			.Build();

		var settings = config.GetRequiredSection("Settings").Get<Settings>();

		if (settings == null)
			throw new Exception("Errore nel caricamento delle impostazioni da appsettings.json.");

		ValidateConfig(settings);
		return settings;
	}

	private static void ValidateConfig(Settings config)
	{
		if (string.IsNullOrEmpty(config.ConnectionStringDatabase))
			throw new Exception("La stringa di connessione al database non è definita.");

		if (!Directory.Exists(config.CsvFolder))
			throw new Exception($"La cartella CSV '{config.CsvFolder}' non esiste.");

		if (!Directory.Exists(config.ArchiveFolder))
			throw new Exception($"La cartella di archivio '{config.ArchiveFolder}' non esiste.");
	}
}
