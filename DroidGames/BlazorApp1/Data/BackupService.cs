using System.IO.Compression;

namespace BlazorApp1.Data;

public class BackupService : BackgroundService
{
    private readonly string _dataDirectory;
    private readonly string _backupDirectory;
    private readonly TimeSpan _backupInterval = TimeSpan.FromMinutes(5);
    private readonly ILogger<BackupService> _logger;

    public BackupService(IConfiguration configuration, ILogger<BackupService> logger)
    {
        _dataDirectory = configuration["DataPath"] ?? "data";
        _backupDirectory = Path.Combine(_dataDirectory, "backups");
        _logger = logger;
        Directory.CreateDirectory(_backupDirectory);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CreateBackup();
                await Task.Delay(_backupInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task CreateBackup()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backupFile = Path.Combine(_backupDirectory, $"backup-{timestamp}.zip");

        using var archive = ZipFile.Open(backupFile, ZipArchiveMode.Create);
        
        foreach (var file in Directory.GetFiles(_dataDirectory, "*.json"))
        {
            archive.CreateEntryFromFile(file, Path.GetFileName(file));
        }

        _logger.LogInformation("Backup created: {BackupFile}", backupFile);

        // Keep only last 50 backups
        var backups = Directory.GetFiles(_backupDirectory, "*.zip")
            .OrderByDescending(f => f)
            .Skip(50);

        foreach (var oldBackup in backups)
        {
            File.Delete(oldBackup);
        }

        await Task.CompletedTask;
    }
}
