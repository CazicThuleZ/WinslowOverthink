namespace DaemonAtorService;

public class LogTokenUsage
{
    public DateTime SnapshotDateUTC { get; set; }
    public string Model { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
