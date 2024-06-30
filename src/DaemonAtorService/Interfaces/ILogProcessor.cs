namespace DaemonAtorService;

public interface ILogProcessor
{
     Task<bool> ProcessAsync(string fileName);
}
