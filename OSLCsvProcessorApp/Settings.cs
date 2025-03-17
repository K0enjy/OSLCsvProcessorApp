using Microsoft.Extensions.Configuration;
using System;

public class Settings
{
	public string ConnectionStringDatabase { get; set; }
	public string CsvFolder { get; set; }
	public string ArchiveFolder { get; set; }

	public static Settings LoadSettings()
	{
		IConfiguration config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false)
			.Build();

		return config.GetRequiredSection("Settings").Get<Settings>() ??
			   throw new Exception("Errore nel caricamento delle impostazioni da appsettings.json.");
	}
}
