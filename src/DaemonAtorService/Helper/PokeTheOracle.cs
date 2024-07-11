using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace DaemonAtorService;

public class PokeTheOracle
{
    private readonly ILogger<PokeTheOracle> _logger;
    private readonly Kernel _kernel;
    private readonly ILoggingStrategy _loggingStrategy;
    private readonly KernelPlugin _emailPluginsFunction;
    private readonly KernelPlugin _journalPluginsFunction;

    private readonly string _kernel_model;

    public PokeTheOracle(ILogger<PokeTheOracle> logger, Kernel kernel, ILoggingStrategy loggingStrategy, IOptions<GlobalSettings> globalSettings)
    {
        _logger = logger;
        _kernel = kernel;
        _loggingStrategy = loggingStrategy;        
        _kernel_model = globalSettings.Value.OpenApiModel;

        var loadedPlugins = _kernel.Plugins.ToList();
        if (loadedPlugins.Any(plugin => plugin.Name.Equals("PolishJournal", StringComparison.OrdinalIgnoreCase)))
            _journalPluginsFunction = _kernel.Plugins.FirstOrDefault(plugin => plugin.Name.Equals("PolishJournal", StringComparison.OrdinalIgnoreCase));
        if (loadedPlugins.Any(plugin => plugin.Name.Equals("InterpretEmails", StringComparison.OrdinalIgnoreCase)))
            _emailPluginsFunction = _kernel.Plugins.FirstOrDefault(plugin => plugin.Name.Equals("InterpretEmails", StringComparison.OrdinalIgnoreCase));
    }
    public async Task<string> InvokeKernelFunctionAsync(string pluginType, string functionName, Dictionary<string, string> arguments)
    {
        KernelPlugin selectedPlugin;
        switch (pluginType.ToLower())
        {
            case "email":
                selectedPlugin = _emailPluginsFunction;
                break;
            case "journal":
                selectedPlugin = _journalPluginsFunction;
                break;
            default:
                selectedPlugin = null;
                break;
        }

        if (_journalPluginsFunction == null)
        {
            _logger.LogInformation($"Requested Semantic Kernel plugin ({pluginType}) not loaded. Aborting. ");
            throw new Exception("Semantic Kernel plugin not loaded.");
        }

        var kernelArguments = new KernelArguments();
        foreach (var arg in arguments)
        {
            kernelArguments.Add(arg.Key, arg.Value);
        }

        var response = await _kernel.InvokeAsync(selectedPlugin[functionName], kernelArguments);
        ProcessTokenUsage(response.Metadata);
        return response.ToString();
    }

    private void ProcessTokenUsage(IReadOnlyDictionary<string, object> metadata)
    {
        if (metadata.ContainsKey("Usage"))
        {
            var usage = (CompletionsUsage)metadata["Usage"];

            LogTokenUsage logTokenUsage = new()
            {
                SnapshotDateUTC = DateTime.UtcNow,
                Model = _kernel_model,
                PromptTokens = usage.PromptTokens,
                CompletionTokens = usage.CompletionTokens,
                TotalTokens = usage.TotalTokens
            };

            _loggingStrategy.Log(logTokenUsage);
        }
    }
}
