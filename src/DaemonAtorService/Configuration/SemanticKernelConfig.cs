using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;

namespace DaemonAtorService;

public static class SemanticKernelConfig
{
    public static Kernel InitializeKernel(IConfiguration configuration)
    {
        var openAiApiKey = configuration["GlobalSettings:OpenApiKey"];
        var openAiApiModel = configuration["GlobalSettings:OpenApiModel"];
        var semanticKernelPluginLocation = configuration["GlobalSettings:SemanticKernelPluginsPath"];

        var builder = Kernel.CreateBuilder();

        builder.Plugins.AddFromType<ConversationSummaryPlugin>();
        builder.Plugins.AddFromPromptDirectory(Path.Combine(semanticKernelPluginLocation, "PolishJournal"));        
        builder.Plugins.AddFromPromptDirectory(Path.Combine(semanticKernelPluginLocation, "InterpretEmails"));
        builder.Services.AddOpenAIChatCompletion(openAiApiModel, openAiApiKey);

        var kernel = builder.Build();

        return kernel;
    }
}
