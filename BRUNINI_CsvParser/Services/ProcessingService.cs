using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace BRUNINI_CsvParser.Services
{
	public interface IProcessingService
	{
		Task ProcessFileAsync(string filePath);
	}
	public class ProcessingService : IProcessingService
	{
		private readonly ICsvParser _csvParser;
		private readonly IDatabaseOperations _databaseOperation;
		private readonly IFileService _fileService;
		private readonly ILogger<ProcessingService> _logger;
		private readonly AsyncRetryPolicy _retryPolicy;
		private readonly Settings _config;

		public ProcessingService(
			ICsvParser csvParser,
			IDatabaseOperations databaseOperation,
			IFileService fileService,
			ILogger<ProcessingService> logger,
			Settings config)
		{
			_csvParser = csvParser;
			_databaseOperation = databaseOperation;
			_fileService = fileService;
			_logger = logger;
			_config = config;

			_retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
					(exception, timeSpan, retryCount, context) =>
					{
						_logger.LogWarning($"Tentativo {retryCount}: errore {exception.Message}. Riprovo tra {timeSpan.TotalSeconds} secondi.");
					});
		}

		public async Task ProcessFileAsync(string filePath)
		{
			try
			{
				_logger.LogInformation($"Inizio elaborazione file: {filePath}");
				var records = _csvParser.ParseCsv(filePath);

				if (!records.Any())
				{
					_logger.LogWarning($"Il file {filePath} non contiene dati validi.");
					return;
				}

				var barcodeUnici = records.Select(r => r.Barcode).Distinct().ToList();

				int idRaccoltaDati = _databaseOperation.CreateRaccoltaDatiTestata(barcodeUnici.First());
				_logger.LogInformation($"Raccolta dati creata con ID: {idRaccoltaDati}");

				var barcodeToDettaglioId = new Dictionary<string, int>();

				foreach (var barcode in barcodeUnici)
				{
					int idDettaglio = _databaseOperation.CreateRaccoltaDatiDettaglio(idRaccoltaDati, barcode);
					barcodeToDettaglioId[barcode] = idDettaglio;
					_logger.LogInformation($"Dettaglio raccolta dati creato con ID: {idDettaglio} per barcode {barcode}");
				}

				foreach (var record in records)
				{
					int idDettaglio = barcodeToDettaglioId[record.Barcode];

					var colonneValide = _databaseOperation.GetColonneValide(record.Barcode);
					var caratteristicheFiltrate = record.Caratteristiche
						.Where(c => colonneValide.Contains(c.Key))
						.ToDictionary(c => c.Key, c => c.Value);

					if (caratteristicheFiltrate.Any())
					{
						_databaseOperation.InsertRaccoltaDatiCicliLavorazioneMisure(idDettaglio, caratteristicheFiltrate, 1, DateTime.Now);
						_logger.LogInformation($"Inserite {caratteristicheFiltrate.Count} misurazioni per il barcode {record.Barcode}");
					}
					else
					{
						_logger.LogWarning($"Nessuna colonna del file {filePath} corrisponde alle caratteristiche nel database. Riga ignorata.");
					}
				}

				_fileService.MoveFileToArchive(filePath, _config.ArchiveFolder);
				_logger.LogInformation($"File {filePath} spostato in {_config.ArchiveFolder}.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Errore durante l'elaborazione del file {filePath}: {ex.Message}", ex);
			}
		}

	}

}

