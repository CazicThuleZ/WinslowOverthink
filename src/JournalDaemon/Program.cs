using System;
using System.IO;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Configuration;
using OpenAI_API.Chat;
using OpenAI_API.Images;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json")
            .Build();

        string inputDirectory = configuration["InputDirectory"];
        string outputDirectory = configuration["OutputDirectory"];
        string openAPIkey = configuration["WinslowKey"];

        // https://github.com/OkGoDoIt/OpenAI-API-dotnet

        // var api = new OpenAI_API.OpenAIAPI(openAPIkey);
        // var result = await api.Completions.GetCompletion("The third rule of fight club is ");
        // Console.WriteLine(result);
        // var result1 = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest("Man in dark sunglasses presenting two contrasting soap bars in a dimly lit, gritty setting, reminiscent of 'Fight Club'.", 1, ImageSize._512));
        // Console.WriteLine(result1.Data[0].Url);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var markdownFiles = Directory.GetFiles(inputDirectory, "*.md");

        List<string> uniqueWords = new List<string>();

        string searchTerm = "My spiritual teacher Mr.";

        foreach (var inputFile in markdownFiles)
        {
            string fileName = Path.GetFileName(inputFile);
            string outputFile = Path.Combine(outputDirectory, fileName);

            string markdownContent = File.ReadAllText(inputFile);

            string[] sections = Regex.Split(markdownContent, @"\n(?=#)");
            foreach (string section in sections)
            {
                var api = new OpenAI_API.OpenAIAPI(openAPIkey);
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

                foreach (string chunk in textChunks)
                {

                    chat.AppendUserInput(chunk);
                    string response = await chat.GetResponseFromChatbotAsync();
                    Console.WriteLine(chunk);
                    Console.WriteLine(response);
                    Console.WriteLine("---------------------------------------------------");
                }
                chat.AppendUserInput("CLASSIFY");
                string hashresponse = await chat.GetResponseFromChatbotAsync();
                Console.WriteLine(hashresponse);
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

        Console.WriteLine("Markdown files processed successfully.");
        Console.WriteLine("Unique Words:");

        foreach (var word in uniqueWords)
        {
            Console.WriteLine(word);
        }
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

}