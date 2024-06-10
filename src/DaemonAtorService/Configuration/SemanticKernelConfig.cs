using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;

namespace DaemonAtorService;

public static class SemanticKernelConfig
{
    public static Kernel InitializeKernel(IConfiguration configuration)
    {
        var openAiApiKey = configuration["GlobalSettings:OpenApiKey"];

        var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.Plugins.AddFromType<ConversationSummaryPlugin>();
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.Services.AddOpenAIChatCompletion("gpt-4o", openAiApiKey);

        var kernel = builder.Build();

        return kernel;
    }
}
