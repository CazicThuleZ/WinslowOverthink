using System;

namespace DaemonAtorService.Models;

public class PostgresBackupSettings 
{
   public List<ServerConfig> Servers { get; set; }
   public string Username { get; set; }
   public string Password { get; set; }
   public string BackupDirectory { get; set; }
}

public class ServerConfig
{
   public string Host { get; set; }
   public int Port { get; set; }
   public Dictionary<string, string> Databases { get; set; }
}
