using Microsoft.Extensions.Logging;

namespace BRUNINI_CsvParser.Services
{
	public interface IFileMonitor
	{
		Task StartMonitoringAsync();
		void StopMonitoring();
	}

	public class FileMonitor : IFileMonitor
	{

		private readonly string _folderPath;
		private readonly IProcessingService _processingService;
		private readonly ILogger<FileMonitor> _logger;
		private FileSystemWatcher _watcher;

		public FileMonitor(IProcessingService processingService, ILogger<FileMonitor> logger, Settings config)
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
}

