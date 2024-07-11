using System.Text.Json;

namespace DaemonAtorService;

public class JsonFileLoggingStrategy : ILoggingStrategy
{
    private readonly string _dasboardDirectory;

    public JsonFileLoggingStrategy(string dasboardDirectory)
    {
        _dasboardDirectory = dasboardDirectory;
    }

    public void Log(object data)
    {
     
        string typeName = data.GetType().Name;     
        string subDirectory = Path.Combine(_dasboardDirectory, typeName);

        Directory.CreateDirectory(subDirectory);

        string fileName = $"{typeName}_{DateTime.Now:yyyyMMddHHmmss}.json";
        string filePath = Path.Combine(subDirectory, fileName);

        string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Write the JSON to the file
        File.WriteAllText(filePath, jsonString);
    }    

}
