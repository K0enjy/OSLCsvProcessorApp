using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static FileMonitorService;

public class MonitorBackgroundService : BackgroundService
{
	private readonly IFileMonitorService _fileMonitorService;
	private readonly ILogger<MonitorBackgroundService> _logger;

	public MonitorBackgroundService(IFileMonitorService fileMonitorService, ILogger<MonitorBackgroundService> logger)
	{
		_fileMonitorService = fileMonitorService;
		_logger = logger;
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Monitor Background Service avviato.");
		_fileMonitorService.StartMonitoring();

		return Task.CompletedTask;
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Monitor Background Service arrestato.");
		_fileMonitorService.StopMonitoring();
		return Task.CompletedTask;
	}
}
