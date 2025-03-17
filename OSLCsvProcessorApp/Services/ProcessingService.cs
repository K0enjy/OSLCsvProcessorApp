using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static CsvParser;
using static DatabaseRepository;
using static FileService;

public class ProcessingService : ProcessingService.IProcessingService
{
	public interface IProcessingService
	{
		Task ProcessFileAsync(string filePath);
	}

	private readonly ICsvParser _csvParser;
	private readonly IDatabaseRepository _databaseRepository;
	private readonly IFileService _fileService;
	private readonly ILogger<ProcessingService> _logger;

	public ProcessingService(
		ICsvParser csvParser,
		IDatabaseRepository databaseRepository,
		IFileService fileService,
		ILogger<ProcessingService> logger)
	{
		_csvParser = csvParser;
		_databaseRepository = databaseRepository;
		_fileService = fileService;
		_logger = logger;
	}

	public async Task ProcessFileAsync(string filePath)
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
			int idRaccoltaDati = _databaseRepository.CreateRaccoltaDatiTestata(barcode);
			_logger.LogInformation($"Raccolta dati creata con ID: {idRaccoltaDati}");

			// Creazione del dettaglio della raccolta dati
			int idDettaglio = _databaseRepository.CreateRaccoltaDatiDettaglio(idRaccoltaDati, barcode);
			_logger.LogInformation($"Dettaglio raccolta dati creato con ID: {idDettaglio}");

			foreach (var record in records)
			{
				DateTime dataOra = record.Data.Add(record.Ora);

				_databaseRepository.InsertRaccoltaDatiCicliLavorazioneMisure(
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
