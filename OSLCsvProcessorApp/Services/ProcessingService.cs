using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using static CsvParser;
using static DatabaseService;
using static FileService;

public class ProcessingService : ProcessingService.IProcessingService
{
	public interface IProcessingService
	{
		void ProcessFile(string filePath);
	}

	private readonly ICsvParser _csvParser;
	private readonly IDatabaseService _databaseService;
	private readonly IFileService _fileService;
	private readonly ILogger<ProcessingService> _logger;

	public ProcessingService(
		ICsvParser csvParser,
		IDatabaseService databaseService,
		IFileService fileService,
		ILogger<ProcessingService> logger)
	{
		_csvParser = csvParser;
		_databaseService = databaseService;
		_fileService = fileService;
		_logger = logger;
	}

	public void ProcessFile(string filePath)
	{
		try
		{
			_logger.LogInformation($"Inizio elaborazione file: {filePath}");

			var records = _csvParser.ParseCsv(filePath);
			if (records.Count == 0)
			{
				_logger.LogWarning($"Il file {filePath} non contiene dati validi.");
				return;
			}

			// Estrarre il barcode di fase dalla colonna 4
			string barcode = records.First().Barcode;
			if (string.IsNullOrEmpty(barcode))
			{
				_logger.LogError($"Barcode di fase non trovato nel file {filePath}. File ignorato.");
				return;
			}

			_logger.LogInformation($"Trovato barcode di fase: {barcode}");

			// Creazione della raccolta dati
			int idRaccoltaDati = _databaseService.CreateRaccoltaDatiTestata(barcode);
			_logger.LogInformation($"Raccolta dati creata con ID: {idRaccoltaDati}");

			// Creazione del dettaglio della raccolta dati
			int idDettaglio = _databaseService.CreateRaccoltaDatiDettaglio(idRaccoltaDati, barcode);
			_logger.LogInformation($"Dettaglio raccolta dati creato con ID: {idDettaglio}");

			foreach (var record in records)
			{
				DateTime dataOra = record.Data.Add(record.Ora);

				_databaseService.InsertRaccoltaDatiCicliLavorazioneMisure(
					idDettaglio,
					record.Caratteristiche,
					record.NumeroMisura,
					dataOra
				);

				_logger.LogInformation($"Inseriti dati di misura per il record con numero misura: {record.NumeroMisura}");
			}

			// Spostamento del file in archivio
			string archiveFolder = "C:\\ArchivioCSV"; // Da parametrizzare in appsettings.json
			_fileService.MoveFileToArchive(filePath, archiveFolder);
			_logger.LogInformation($"File {filePath} spostato in {archiveFolder}");

		}
		catch (Exception ex)
		{
			_logger.LogError($"Errore durante l'elaborazione del file {filePath}: {ex.Message}", ex);
		}
	}
}
