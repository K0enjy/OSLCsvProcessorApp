using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
	private Timer _timer;

	public FileMonitorService(IProcessingService processingService, ILogger<FileMonitorService> logger, IConfiguration configuration)
	{
		_folderPath = configuration["FileSettings:CsvFolder"];
		_processingService = processingService;
		_logger = logger;
	}

	public async Task StartMonitoringAsync()
	{
		_logger.LogInformation($"Monitoraggio avviato sulla cartella: {_folderPath}");
		_timer = new Timer(async _ => await CheckForNewFilesAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
	}

	private async Task CheckForNewFilesAsync()
	{
		var files = Directory.GetFiles(_folderPath, "*.csv");
		foreach (var file in files)
		{
			_logger.LogInformation($"Trovato file: {file}");
			await _processingService.ProcessFileAsync(file);
		}
	}

	public void StopMonitoring()
	{
		_timer?.Dispose();
		_logger.LogInformation("Monitoraggio interrotto.");
	}
}
