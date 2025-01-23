using System;
using System.Diagnostics;
using System.Text;
using DaemonAtorService.Models;
using Microsoft.Extensions.Options;

namespace DaemonAtorService.Helper;

public class PostgresBackupService
{
   private readonly ILogger<PostgresBackupService> _logger;
   private readonly PostgresBackupSettings _settings;

   public PostgresBackupService(ILogger<PostgresBackupService> logger, IOptions<PostgresBackupSettings> settings)
   {
       _logger = logger;
       _settings = settings.Value;
   }

   public async Task PerformBackups()
   {
       try
       {
           Directory.CreateDirectory(_settings.BackupDirectory);
           var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

           foreach(var server in _settings.Servers)
           {
               foreach(var database in server.Databases)
               {
                   await CreateBackup(
                       server.Host, 
                       server.Port,
                       database.Key,
                       Path.Combine(_settings.BackupDirectory, $"{database.Key}_backup_{timestamp}.sql")
                   );
               }
           }
       }
       catch (Exception ex)
       {
           _logger.LogError($"Failed to perform backups: {ex.Message}");
           throw;
       }
   }

   private async Task CreateBackup(string host, int port, string database, string outputFile)
   {
       try
       {
           var startInfo = new ProcessStartInfo
           {
               FileName = "pg_dump",
               Arguments = $"-h {host} -p {port} -U {_settings.Username} -F p -f \"{outputFile}\" {database}",
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               UseShellExecute = false,
               CreateNoWindow = true
           };

           startInfo.EnvironmentVariables["PGPASSWORD"] = _settings.Password;

           using var process = new Process { StartInfo = startInfo };
           await RunProcessAsync(process);
           _logger.LogInformation($"Successfully created backup for {database} at {outputFile}");
       }
       catch (Exception ex)
       {
           _logger.LogError($"Failed to create backup for {database}: {ex.Message}");
           throw;
       }
   }

   private async Task RunProcessAsync(Process process)
   {
       var standardOutput = new StringBuilder();
       var standardError = new StringBuilder();

       process.OutputDataReceived += (sender, args) =>
       {
           if (!string.IsNullOrEmpty(args.Data))
           {
               standardOutput.AppendLine(args.Data);
               _logger.LogInformation($"Process output: {args.Data}");
           }
       };
       process.ErrorDataReceived += (sender, args) =>
       {
           if (!string.IsNullOrEmpty(args.Data))
           {
               standardError.AppendLine(args.Data);
               _logger.LogError($"Process error: {args.Data}");
           }
       };

       process.Start();
       process.BeginOutputReadLine();
       process.BeginErrorReadLine();
       await process.WaitForExitAsync();

       if (process.ExitCode != 0)
           throw new Exception($"Process exited with code {process.ExitCode}. Error: {standardError}");
   }
}