using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI_API.Models;

namespace DaemonDo.Journal.AutoClassify;

public class AutoClassify : IHostedService
{
    private ILogger<AutoClassify> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly string _inputDirectory;
    private readonly string _outputDirectory;
    private readonly string _saveHashtagsFile;
    private readonly string _saveHyperlinksFile;
    private readonly string _openApiKey;
    private readonly string _invalidChars;
    public List<HashTagXref> _hashTagLookup { get; set; }
    public int _tokenCount { get; set; }
    public List<Hyperlinkage> _hyperlinkage { get; set; }

    public AutoClassify(ILogger<AutoClassify> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _logger = logger;
        _clientFactory = httpClientFactory;
        _configuration = config;

        _inputDirectory = _configuration.GetValue<string>("InputDirectory");
        _outputDirectory = _configuration.GetValue<string>("OutputDirectory");
        _openApiKey = _configuration.GetValue<string>("WinslowKey");
        _saveHashtagsFile = _configuration.GetValue<string>("HashtagsFile");
        _saveHyperlinksFile = _configuration.GetValue<string>("HyperLinksFile");

        _hashTagLookup = LoadHashTags(_saveHashtagsFile);
        _hyperlinkage = LoadHyperlinks(_saveHyperlinksFile);

        _invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        _tokenCount = 0;

    }

    private List<Hyperlinkage> LoadHyperlinks(string saveHyperlinksFile)
    {
        if (File.Exists(saveHyperlinksFile))
        {
            string json = File.ReadAllText(saveHyperlinksFile);
            return JsonSerializer.Deserialize<List<Hyperlinkage>>(json);
        }
        else
        {
            return new List<Hyperlinkage>();
        }
    }

    private async Task ParseNewMusings()
    {
        await FindUnprocessedContent();
    }

    private async Task FindUnprocessedContent()
    {
        if (!Directory.Exists(_outputDirectory))
            Directory.CreateDirectory(_outputDirectory);

        var inputFiles = Directory.GetFiles(_inputDirectory, "*.md")
                                  .Where(f => !Path.GetFileName(f).StartsWith("upchuck", StringComparison.OrdinalIgnoreCase))
                                  .OrderByDescending(f => Path.GetFileName(f))
                                  .ToArray();

        int count = 0;
        foreach (var inputFile in inputFiles)
        {
            string fileName = Path.GetFileName(inputFile);
            if (File.Exists(_inputDirectory + @"/" + fileName))
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
                    List<string> textChunks = SplitTextIntoChunks(textContent, 4000);
                    if (textChunks.Count > 0)
                    {
                        try
                        {
                            var polishedUpchuck = await PolishThisUpchuck(textChunks);
                            polishedUpchuck.CaptureDateSlug = dateSlug;
                            polishedUpchuck.GeneratedFromFileName = "upchuck" + fileName;
                            SavePolishedUpchuckToFile(polishedUpchuck, "polishedUpchuck" + outputFileName);
                            dateSlug = string.Empty;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error processing file: " + inputFile + ":::" + ex.Message);
                        }
                    }

                    SaveHashtagsToFile(_saveHashtagsFile);

                }

                // string htmlContent = Markdown.ToHtml(markdownContent);

                // Rename so it doesn't get processed again in future runs.
                string newFileName = "upchuck" + fileName;
                string newFilePath = Path.Combine(Path.GetDirectoryName(inputFile), newFileName);
                try
                {
                    File.Move(inputFile, newFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error renaming input file ::: " +  newFileName + " ::: " + inputFile + " ::: " + " to output file " + " ::: " + newFilePath + ex.Message);
                }
            }

            count++;
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("Processed " + count.ToString() + " of " + inputFiles.Length.ToString() + " files.");
            Console.WriteLine("---------------------------------------------------");
            if (count >= 50)
                break;
        }
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

        string filePath = Path.Combine(_outputDirectory, outputFile);
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
            filePath = Path.Combine(_outputDirectory, newFileName);
        }

        throw new Exception("Could not write to file after 10 attempts.");
    }

    private async Task<PolishedUpchuck> PolishThisUpchuck(List<string> textChunks)
    {
        // https://github.com/OkGoDoIt/OpenAI-API-dotnet
        var api = new OpenAI_API.OpenAIAPI(_openApiKey);
        var chat = api.Chat.CreateConversation();
        chat.Model = Model.ChatGPTTurbo_16k;

        PolishedUpchuck polishedUpchuck = new PolishedUpchuck();

        chat.AppendSystemMessage(@"You are an analyst classifying blocks of text according to their content. I will provide a series of inputs for which you will respond ""Understood"".  When I am finished providing imputs, I will send a single input command ""CLASSIFY"" at which time you will respond with exactly 5 hashtags that best represents the content of the series.");

        foreach (string chunk in textChunks)
        {
            chat.AppendUserInput(chunk);
            string response = await chat.GetResponseFromChatbotAsync();
            _tokenCount += chat.MostRecentApiResult.Usage.PromptTokens;
            await Task.Delay(TimeSpan.FromSeconds(20));
            Console.WriteLine(chunk);
            Console.WriteLine(response);
        }
        chat.AppendUserInput("CLASSIFY");
        try
        {
            string response = await chat.GetResponseFromChatbotAsync();
            _tokenCount += chat.MostRecentApiResult.Usage.PromptTokens;
            await Task.Delay(TimeSpan.FromSeconds(20));
            polishedUpchuck.Hashtags = ParseHashtags(response);
            Console.WriteLine(response);
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("Token Count:  " + _tokenCount.ToString() + "   Cost:  " + ((_tokenCount / 1000) * .0020).ToString());
            Console.WriteLine("---------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        try
        {
            chat.AppendSystemMessage(@"You are a talented New York Times editor. Rewrite the previous with well-structured, well paragraphed and well punctuated sentences using your expertise in grammer and style.  Avoid redundancy and avoid summarizing.  Ensure that each sentence contributes to the spirit of what the original piece is trying to say.  Do not mention your role or goals in your response.");
            string response = await chat.GetResponseFromChatbotAsync();
            _tokenCount += chat.MostRecentApiResult.Usage.PromptTokens;
            response = MarkupHyperlinks(response);
            polishedUpchuck.Upchuck = response;
            await Task.Delay(TimeSpan.FromSeconds(10));
            Console.WriteLine(response);
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("Token Count:  " + _tokenCount.ToString() + "   Cost:  " + ((_tokenCount / 1000) * .0020).ToString());
            Console.WriteLine("---------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return polishedUpchuck;

    }

    private string MarkupHyperlinks(string response)
    {
        foreach (var hyperlinkage in _hyperlinkage)
        {
            response = Regex.Replace(response, hyperlinkage.SourceMapping, hyperlinkage.TargetMapping, RegexOptions.IgnoreCase);
        }

        return response;
    }

    static List<string> SplitTextIntoChunks(string text, int chunkSize)
    {
        List<string> chunks = new List<string>();
        int startIndex = 0;

        while (startIndex < text.Length)
        {
            int endIndex = Math.Min(startIndex + chunkSize, text.Length);
            string chunk = text.Substring(startIndex, endIndex - startIndex);
            chunks.Add(chunk);
            startIndex = endIndex;
        }

        return chunks;
    }

    static List<HashTagXref> LoadHashTags(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<HashTagXref>>(json);
        }
        else
        {
            return new List<HashTagXref>();
        }
    }

    private List<string> ParseHashtags(string hashtags)
    {
        var hashtagParts = hashtags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> hashtagsToApply = new HashSet<string>();

        foreach (var hashtagPart in hashtagParts)
        {
            string hashtag = hashtagPart.TrimStart('#').ToLower();

            string alias = GetAliasOrAddTag(hashtag);
            if (alias != string.Empty)
                hashtagsToApply.Add(alias);
        }

        return hashtagsToApply.ToList();
    }

    public void SaveHashtagsToFile(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };

        string jsonString = JsonSerializer.Serialize(_hashTagLookup, options);
        File.WriteAllText(filePath, jsonString);
    }


    private string GetAliasOrAddTag(string tag)
    {
        var hashTagXref = _hashTagLookup.FirstOrDefault(x => x.HashTag == tag);
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ParseNewMusings();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
