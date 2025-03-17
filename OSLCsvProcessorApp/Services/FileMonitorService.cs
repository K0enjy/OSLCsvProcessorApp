using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static ProcessingService;

public class FileMonitorService : FileMonitorService.IFileMonitorService
{
	public interface IFileMonitorService
	{
		Task StartMonitoringAsync();
		void StopMonitoring();
	}

	private readonly string _folderPath;
	private readonly IProcessingService _processingService;
	private readonly ILogger<FileMonitorService> _logger;
	private FileSystemWatcher _watcher;

	public FileMonitorService(IProcessingService processingService, ILogger<FileMonitorService> logger, AppConfig config)
	{
		_folderPath = config.CsvFolder;
		_processingService = processingService;
		_logger = logger;
	}

	public async Task StartMonitoringAsync()
	{
		_logger.LogInformation($"Avvio monitoraggio cartella: {_folderPath}");

		_watcher = new FileSystemWatcher(_folderPath)
		{
			Filter = "*.csv",
			EnableRaisingEvents = true
		};

		_watcher.Created += async (s, e) => await ProcessFileAsync(e.FullPath);
	}

	private async Task ProcessFileAsync(string filePath)
	{
		_logger.LogInformation($"Nuovo file rilevato: {filePath}");
		await _processingService.ProcessFileAsync(filePath);
	}

	public void StopMonitoring()
	{
		_watcher?.Dispose();
		_logger.LogInformation("Monitoraggio interrotto.");
	}
}
