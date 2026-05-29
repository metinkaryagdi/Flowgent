namespace BitirmeProject.AiService.Application.Abstractions;

/// <summary>
/// Runtime'da fine-tune ↔ base model arasında geçişi tutar.
/// Demo amaçlı UI üzerinden toggle edilebilir; her Ollama çağrısında okunur.
/// </summary>
public interface IModelSelector
{
    string BaseModel { get; }
    string FinetunedModel { get; }
    string FallbackModel { get; }
    bool UseFinetuned { get; }

    /// <summary>Şu anki aktif model adı (UseFinetuned'a göre).</summary>
    string ActiveModel { get; }

    void SetUseFinetuned(bool useFinetuned);
}
