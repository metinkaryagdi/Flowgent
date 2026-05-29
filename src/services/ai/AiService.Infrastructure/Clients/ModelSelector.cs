using BitirmeProject.AiService.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace BitirmeProject.AiService.Infrastructure.Clients;

public sealed class ModelSelector : IModelSelector
{
    private int _useFinetuned;

    public ModelSelector(IConfiguration configuration)
    {
        BaseModel = configuration["Ollama:Model"] ?? "gemma3:4b";
        FinetunedModel = configuration["Ollama:FinetunedModel"] ?? "bp-agent";
        var configuredFallback = configuration["Ollama:FallbackModel"];
        FallbackModel = string.IsNullOrWhiteSpace(configuredFallback) ? BaseModel : configuredFallback;

        var initial = configuration.GetValue("Ollama:UseFinetuned", false);
        _useFinetuned = initial ? 1 : 0;
    }

    public string BaseModel { get; }
    public string FinetunedModel { get; }
    public string FallbackModel { get; }

    public bool UseFinetuned => Volatile.Read(ref _useFinetuned) == 1;

    public string ActiveModel => UseFinetuned ? FinetunedModel : BaseModel;

    public void SetUseFinetuned(bool useFinetuned)
    {
        Volatile.Write(ref _useFinetuned, useFinetuned ? 1 : 0);
    }
}
