using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace DaemonAtorService;

[DisallowConcurrentExecution]
public class DirectorySyncJob : IJob
{
    private readonly ILogger<DirectorySyncJob> _logger;
    private readonly List<DirectorySyncSettings> _syncSettings;

    public int _syncedFileCount { get; set; }

    public DirectorySyncJob(ILogger<DirectorySyncJob> logger, IOptions<List<DirectorySyncSettings>> syncSettings)
    {
        _logger = logger;
        _syncSettings = syncSettings.Value;
    }

    public Task Execute(IJobExecutionContext context)
    {

        _logger.LogInformation("DirectorySyncJob started at {time}", DateTimeOffset.Now);
        foreach (var setting in _syncSettings)
        {
            try
            {
                string sourceDirectory = setting.SourceDirectory;
                string destinationDirectory = setting.DestinationDirectory;
                bool mirrorStructure = setting.MirrorDirectoryStructure;

                if (!Directory.Exists(destinationDirectory))
                    Directory.CreateDirectory(destinationDirectory);

                var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(sourceDirectory, file);
                    var destFile = mirrorStructure ? Path.Combine(destinationDirectory, relativePath) : Path.Combine(destinationDirectory, Path.GetFileName(file));

                    var destDir = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);

                    if (!File.Exists(destFile))
                    {
                        File.Copy(file, destFile, false);
                        _syncedFileCount++;
                    }
                }

                _logger.LogInformation($"Directory synchronization completed successfully for source: {sourceDirectory} to destination: {destinationDirectory}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during directory synchronization for source: {setting.SourceDirectory} to destination: {setting.DestinationDirectory}. Error: {ex.Message}");
            }
        }

        _logger.LogInformation($"DirectorySyncJob syncronized {_syncedFileCount} files.");

        return Task.CompletedTask;
    }
}
