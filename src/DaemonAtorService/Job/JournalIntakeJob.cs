using Quartz;
using RestSharp;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DaemonAtorService;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.Text;
using Azure.AI.OpenAI;

namespace DaemonAtorService;

[DisallowConcurrentExecution]
public class JournalIntakeJob : IJob
{
    private readonly ILogger<JournalIntakeJob> _logger;
    private readonly string _inputFilePath;
    private readonly string _OutputFilePath;
    public List<Hyperlinkage> _hyperlinkage { get; set; }
    public List<HashTagXref> _hashTagLookup { get; set; }
    private readonly string _saveHashtagsFile;
    private readonly string _saveHyperlinksFile;
    private readonly string _invalidChars;
    public readonly string _dashboardLogLocation;

    private readonly PokeTheOracle _pokeTheOracle;
    public readonly string _openApiModel;
    public JournalIntakeJob(ILogger<JournalIntakeJob> logger, IOptions<JournalSettings> settings, IOptions<GlobalSettings> globalSettings, PokeTheOracle pokeTheOracle)
    {
        _logger = logger;
        _inputFilePath = settings.Value.InputDirectory;
        _OutputFilePath = settings.Value.OutputDirectory;
        _saveHashtagsFile = settings.Value.HashtagsFile;
        _saveHyperlinksFile = settings.Value.HyperLinksFile;
        _dashboardLogLocation = globalSettings.Value.DashboardLogLocation;
        _openApiModel = globalSettings.Value.OpenApiModel;

        _hashTagLookup = LoadJsonList<HashTagXref>(_saveHashtagsFile);
        _hyperlinkage = LoadJsonList<Hyperlinkage>(_saveHyperlinksFile);

        _pokeTheOracle = pokeTheOracle;

        _invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Journal Intake Job started at: {time}", DateTimeOffset.Now);

        try
        {
            if (!Directory.Exists(_OutputFilePath))
                Directory.CreateDirectory(_OutputFilePath);

            var inputFiles = Directory.GetFiles(_inputFilePath, "*.md")
                                      .Where(f => !Path.GetFileName(f).StartsWith("upchuck", StringComparison.OrdinalIgnoreCase))
                                      .OrderByDescending(f => Path.GetFileName(f))
                                      .ToArray();

            foreach (var inputFile in inputFiles)
            {
                string fileName = Path.GetFileName(inputFile);
                var lastModified = File.GetLastWriteTime(inputFile);
                string yearMonthFolder = lastModified.ToString("yyyyMM");
                string polishedSubFolderPath = Path.Combine(_inputFilePath, "upchuck", "polished", yearMonthFolder);
                string unPolishedSubFolderPath = Path.Combine(_inputFilePath, "upchuck", "raw", yearMonthFolder);

                if (!Directory.Exists(polishedSubFolderPath))
                    Directory.CreateDirectory(polishedSubFolderPath);

                if (!Directory.Exists(unPolishedSubFolderPath))
                    Directory.CreateDirectory(unPolishedSubFolderPath);                    

                if (File.Exists(_inputFilePath + @"/" + fileName) &&
                    !File.Exists(unPolishedSubFolderPath + @"/" + fileName))  // Prevent processing of duplicates.
                {
                    string markdownContent = File.ReadAllText(inputFile);
                    string dateSlug = string.Empty;

                    string[] sections = Regex.Split(markdownContent, @"\n(?=#)");
                    foreach (string section in sections)
                    {
                        string outputFileName = string.Empty;
                        string sectionContent = section.Trim();

                        string[] lines = sectionContent.Split('\n');
                        List<string> filteredLines = new List<string>();

                        foreach (string line in lines)
                        {
                            if (line.Trim().ToUpper().StartsWith("# DIALOG"))
                            {
                                Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(_invalidChars)));
                                outputFileName = regex.Replace(line, "") + ".md";
                            }

                            if (line.Trim().ToUpper().Contains("DATES::"))
                                dateSlug = line.Trim();

                            if (!line.Trim().StartsWith("#") &&
                                !line.Trim().StartsWith("dates::") &&
                                !line.Trim().StartsWith("tags::") &&
                                 line.Trim() != string.Empty)
                            {
                                filteredLines.Add(line);
                            }
                        }

                        string textContent = string.Join("\n", filteredLines);
                        if (!string.IsNullOrEmpty(textContent))
                        {
                            var polishedUpchuck = await PolishUpchuck(textContent);
                            polishedUpchuck.CaptureDateSlug = dateSlug;
                            polishedUpchuck.GeneratedFromFileName = "upchuck" + fileName;
                            SavePolishedUpchuckToFile(polishedUpchuck, Path.Combine(polishedSubFolderPath, "polishedUpchuck" + outputFileName));
                            dateSlug = string.Empty;
                        }

                        SaveHashtagsToFile(_saveHashtagsFile);
                    }

                    // Rename so it doesn't get processed again in future runs.
                    string newFileName = "upchuck" + fileName;
                    string newFilePath = Path.Combine(unPolishedSubFolderPath, newFileName);
                    try
                    {
                        File.Move(inputFile, newFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Error renaming input file ::: " + newFileName + " ::: " + inputFile + " ::: " + " to output file " + " ::: " + newFilePath + ex.Message);
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing journal files.");
        }

        _logger.LogInformation("Journal Intake Job completed at: {time}", DateTimeOffset.Now);
    }


    private List<T> LoadJsonList<T>(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json);
        }
        else
        {
            return new List<T>();
        }
    }

    private async Task<PolishedUpchuck> PolishUpchuck(string textChunk)
    {
        PolishedUpchuck polishedUpchuck = new PolishedUpchuck();

        // Apply hashtags
        var categorizeResponse = await _pokeTheOracle.InvokeKernelFunctionAsync("journal", "CatagorizeContent", new Dictionary<string, string> { { "journalEntry", textChunk } });
        // proofread
        var proofreadResponse = await _pokeTheOracle.InvokeKernelFunctionAsync("journal", "ProofreadGrammer", new Dictionary<string, string> { { "journalEntry", textChunk } });

        polishedUpchuck.Hashtags = ParseHashtags(categorizeResponse);
        polishedUpchuck.Upchuck = MarkupHyperlinks(proofreadResponse);

        return polishedUpchuck;
    }

    private void SavePolishedUpchuckToFile(PolishedUpchuck polishedUpchuck, string outputFile)
    {
        var lines = new List<string>
        {
            polishedUpchuck.CaptureDateSlug,
            "up:: [[" + polishedUpchuck.GeneratedFromFileName + "]]",
            "tags:: " + string.Join(" ", polishedUpchuck.Hashtags.Select(tag => "#" + tag)),
            polishedUpchuck.Upchuck
        };

        string filePath = Path.Combine(_OutputFilePath, outputFile);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFile);
        string fileExtension = Path.GetExtension(outputFile);
        Random random = new Random();

        for (int i = 0; i < 10; i++)
        {
            if (!File.Exists(filePath))
            {
                File.WriteAllLines(filePath, lines);
                return;
            }

            char randomChar = (char)random.Next('a', 'z' + 1);
            string newFileName = randomChar + fileNameWithoutExtension + fileExtension;
            filePath = Path.Combine(_OutputFilePath, newFileName);
        }

        throw new Exception("Could not write to file after 10 attempts.");
    }

    private List<string> ParseHashtags(string hashtags)
    {

        hashtags = Regex.Replace(hashtags, @"[^a-zA-Z0-9 ]", string.Empty);
        var hashtagParts = hashtags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> hashtagsToApply = new HashSet<string>();

        foreach (var hashtagPart in hashtagParts)
        {
            string alias = GetAliasOrAddTag(hashtagPart);
            if (alias != string.Empty)
                hashtagsToApply.Add(alias);
        }

        return hashtagsToApply.ToList();
    }

    private string GetAliasOrAddTag(string tag)
    {
        var hashTagXref = _hashTagLookup.FirstOrDefault(x => string.Equals(x.HashTag, tag, StringComparison.OrdinalIgnoreCase));
        if (hashTagXref != null)
        {
            return hashTagXref.HashTagAlias;
        }
        else
        {
            _hashTagLookup.Add(new HashTagXref { HashTag = tag, HashTagAlias = "" });
            return "";
        }
    }
    public void SaveHashtagsToFile(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };

        string jsonString = JsonSerializer.Serialize(_hashTagLookup, options);

        File.WriteAllText(filePath, jsonString);
    }
    private string MarkupHyperlinks(string response)
    {
        foreach (var hyperlinkage in _hyperlinkage)
        {
            response = Regex.Replace(response, hyperlinkage.SourceMapping, hyperlinkage.TargetMapping, RegexOptions.IgnoreCase);
        }

        return response;
    }

    private void LogForDashboard(string text, string subject, string logLocation, string subDirectory)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(logLocation) || string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Text, log location must be provided.");

        string subDirectoryPath = Path.Combine(logLocation, subDirectory);

        if (!Directory.Exists(subDirectoryPath))
            Directory.CreateDirectory(subDirectoryPath);

        string date = DateTime.Now.ToString("yyyyMMddHHmmss");  // Uses 24-hour format
        string logFileName = $"{date}-{subject}.txt";
        string logFilePath = Path.Combine(subDirectoryPath, logFileName);

        using (StreamWriter writer = new StreamWriter(logFilePath, false))  // Overwrite if exists
        {
            writer.WriteLine($"{DateTime.Now}: {text}");
        }
    }
    // private void ProcessTokenUsage(IReadOnlyDictionary<string, object> metadata)
    // {
    //     if (metadata.ContainsKey("Usage"))
    //     {
    //         var usage = (CompletionsUsage)metadata["Usage"];
    //         _totalOutputTokens += usage.CompletionTokens;
    //         _totalPromptTokens += usage.PromptTokens;
    //         _logger.LogInformation($"Token usage. Input tokens: {usage.PromptTokens}; Output tokens: {usage.CompletionTokens}");
    //     }
    // }
    // private async Task<string> InvokeKernelFunctionAsync(string functionName, string textChunk)
    // {
    //     var arguments = new KernelArguments() { { "journalEntry", textChunk }, { "logEntry", textChunk } };
    //     var response = await _kernel.InvokeAsync(_journalPluginsFunction[functionName], arguments);
    //     ProcessTokenUsage(response.Metadata);
    //     return response.ToString();
    // }
}