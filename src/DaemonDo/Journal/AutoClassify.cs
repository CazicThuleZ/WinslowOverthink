﻿using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DaemonDo.Journal.AutoClassify;

public class AutoClassify : IHostedService
{
    private ILogger<AutoClassify> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly string _inputDirectory;
    private readonly string _outputDirectory;
    private readonly string _saveHashtagsFile;
    private readonly string _openApiKey;
    private Dictionary<string, HashtagInfo> _hashtagsDictionary = new Dictionary<string, HashtagInfo>();

    public AutoClassify(ILogger<AutoClassify> logger, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _logger = logger;
        _clientFactory = httpClientFactory;
        _configuration = config;

        _inputDirectory = _configuration.GetValue<string>("InputDirectory");
        _outputDirectory = _configuration.GetValue<string>("OutputDirectory");
        _openApiKey = _configuration.GetValue<string>("WinslowKey");
        _saveHashtagsFile = _configuration.GetValue<string>("HashtagsFile");

    }

    private async Task ParseNewMusings()
    {
        //LoadHashTags();
        await FindUnprocessedContent();
    }

    private async Task FindUnprocessedContent()
    {
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        var markdownFiles = Directory.GetFiles(_inputDirectory, "*.md");

        List<string> uniqueWords = new List<string>();

        string searchTerm = "My spiritual teacher Mr.";
        foreach (var inputFile in markdownFiles)
        {
            string fileName = Path.GetFileName(inputFile);
            string outputFile = Path.Combine(_outputDirectory, fileName);

            string markdownContent = File.ReadAllText(inputFile);

            string[] sections = Regex.Split(markdownContent, @"\n(?=#)");
            foreach (string section in sections)
            {
                // https://github.com/OkGoDoIt/OpenAI-API-dotnet
                var api = new OpenAI_API.OpenAIAPI(_openApiKey);
                var chat = api.Chat.CreateConversation();

                chat.AppendSystemMessage(@"You are an analyst classifying blocks of text according to their content. I will provide a series of inputs for which you will respond ""Understood"".  When I am finished providing imputs, I will send a single input command ""CLASSIFY"" at which time you will respond with exactly 5 hashtags that best represents the content of the series.");
                string sectionContent = section.Trim();

                // Ignore lines beginning with #
                string[] lines = sectionContent.Split('\n');
                List<string> filteredLines = new List<string>();

                foreach (string line in lines)
                {
                    if (!line.Trim().StartsWith("#") &&
                        !line.Trim().StartsWith("dates::") &&
                        !line.Trim().StartsWith("tags::"))
                    {
                        filteredLines.Add(line);
                    }
                }

                string textContent = string.Join("\n", filteredLines);
                List<string> textChunks = SplitTextIntoChunks(textContent, 4000);

                if (textChunks.Count > 0)
                {
                    foreach (string chunk in textChunks)
                    {
                        chat.AppendUserInput(chunk);
                        string response = await chat.GetResponseFromChatbotAsync();
                        Console.WriteLine(chunk);
                        Console.WriteLine(response);
                        Console.WriteLine("---------------------------------------------------");
                    }
                    chat.AppendUserInput("CLASSIFY");
                    try
                    {
                        string response = await chat.GetResponseFromChatbotAsync();
                        ParseAndCountHashtags(response);
                        Console.WriteLine(response);
                        Console.WriteLine("---------------------------------------------------");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            //chat.AppendUserInput(markdownContent);            
            //string response = await chat.GetResponseFromChatbotAsync();
            //Console.WriteLine(response);

            // the entire chat history is available in chat.Messages
            //foreach (ChatMessage msg in chat.Messages)
            //{
            //    Console.WriteLine($"{msg.Role}: {msg.Content}");
            //}

            string htmlContent = Markdown.ToHtml(markdownContent);
            ExtractAndAddWords(markdownContent, uniqueWords, searchTerm);



            //File.WriteAllText(outputFile, htmlContent);
        }

        SaveHashtagsToFile(_saveHashtagsFile);
        Console.WriteLine("Markdown files processed successfully.");
        // Console.WriteLine("Unique Words:");

        // foreach (var word in uniqueWords)
        // {
        //     Console.WriteLine(word);
        // }
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

    private void LoadHashTags()
    {
        throw new NotImplementedException();
    }
    public void ParseAndCountHashtags(string hashtags)
    {
        var hashtagParts = hashtags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var hashtagPart in hashtagParts)
        {
            string hashtag = hashtagPart.TrimStart('#').ToLower();

            if (_hashtagsDictionary.TryGetValue(hashtag, out HashtagInfo existingEntry))
                existingEntry.Count++;
            else
                _hashtagsDictionary.Add(hashtag, new HashtagInfo(hashtag));
        }
    }

    public void SaveHashtagsToFile(string filePath)
    {
        // Serialize the dictionary to JSON, we only need the values which are the HashtagInfo objects
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(_hashtagsDictionary.Values, options);

        // Write the JSON string to file
        File.WriteAllText(filePath, jsonString);
    }

    static void ExtractAndAddWords(string content, List<string> uniqueWords, string searchTerm)
    {
        searchTerm = searchTerm.ToLower();
        content = content.ToLower();

        int index = content.IndexOf(searchTerm);

        while (index != -1)
        {
            // Move index to the end of the search term
            index += searchTerm.Length;

            // Extract the next two words
            var words = content
                .Substring(index)
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .ToList();

            if (words.Count == 2)
            {
                string twoWords = string.Join(" ", words);
                // Add unique words to the list
                if (!uniqueWords.Contains(twoWords))
                {
                    uniqueWords.Add(twoWords);
                }
            }

            // Find the next occurrence
            index = content.IndexOf(searchTerm, index);
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
