namespace DaemonAtorService;

public class DirectorySyncSettings
{
    public string SourceDirectory { get; set; }
    public string DestinationDirectory { get; set; }
    public bool MirrorDirectoryStructure { get; set; }
    public List<string> FilePatterns { get; set; }
    public bool IncludeSubdirectories { get; set; } = true;
}

