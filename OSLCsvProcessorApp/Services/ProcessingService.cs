using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
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
	private readonly AsyncRetryPolicy _retryPolicy;

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
		await _retryPolicy.ExecuteAsync(async () =>
		{
			_logger.LogInformation($"Elaborazione file: {filePath}");
			var records = _csvParser.ParseCsv(filePath);

			// Elaborazione dei record...
			await Task.CompletedTask;
		});
	}
}
