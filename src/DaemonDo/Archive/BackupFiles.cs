using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DaemonDo.Archive
{
    public class BackupFiles : IHostedService
    {
        private ILogger<BackupFiles> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _backupManifestFile;
        public List<BackupManifest> _backupManifests { get; set; }

        public BackupFiles(ILogger<BackupFiles> logger, IConfiguration config)
        {
            _logger = logger;
            _configuration = config;

            _backupManifestFile = _configuration.GetValue<string>("BackupManifestFile");

            _backupManifests = LoadBackupManifests(_backupManifestFile);

        }

        private List<BackupManifest> LoadBackupManifests(string backupManifestFile)
        {
            if (File.Exists(backupManifestFile))
            {
                string json = File.ReadAllText(backupManifestFile);
                return JsonSerializer.Deserialize<List<BackupManifest>>(json);
            }
            else
            {
                return new List<BackupManifest>();
            }
        }

        private async Task BackItUp()
        {
            await BackupDesignatedFiles();
        }

        private async Task BackupDesignatedFiles()
        {
            foreach (var backupManifest in _backupManifests)
            {
                try
                {
                    await Task.Delay(1000);
                    var sourceFiles = Directory.EnumerateFiles(backupManifest.Directory);
                    foreach (var sourceFile in sourceFiles)
                    {
                        var fileName = Path.GetFileName(sourceFile);
                        Console.WriteLine(fileName);
                    }
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine(ioEx.Message);
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await BackItUp();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}