using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;

namespace Amnesia.Llm;

/// I parametri con cui si parla al provider. Sono dati di contenuto, non
/// costanti compilate: cambiare modello e' una modifica al gioco, non al motore.
public sealed record LlmConfig
{
    [JsonPropertyName("primary_model")]
    public string PrimaryModel { get; init; } = "";

    [JsonPropertyName("fallback_model")]
    public string FallbackModel { get; init; } = "";

    [JsonPropertyName("request_timeout_sec")]
    public int RequestTimeoutSec { get; init; } = 45;

    [JsonPropertyName("max_tool_passes")]
    public int MaxToolPasses { get; init; } = 4;

    public static Result<LlmConfig> FromJson(string text)
    {
        LlmConfig? config;
        try
        {
            using var document = JsonDocument.Parse(text);
            // Il tipo di errore va distinto: "non e' JSON" e "e' JSON ma non un
            // oggetto" si correggono in due modi diversi.
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Result<LlmConfig>.Fail("invalid_config", "la configurazione llm deve essere un oggetto JSON");
            }
            config = document.RootElement.Deserialize<LlmConfig>();
        }
        catch (JsonException)
        {
            return Result<LlmConfig>.Fail("invalid_config", "la configurazione llm deve essere JSON valido");
        }

        if (config is null || config.PrimaryModel.Length == 0)
        {
            return Result<LlmConfig>.Fail("invalid_config", "primary_model e' obbligatorio");
        }

        return Result<LlmConfig>.Ok(config);
    }

    public static Result<LlmConfig> Load(string path)
    {
        try
        {
            return FromJson(File.ReadAllText(path));
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            return Result<LlmConfig>.Fail("invalid_config", $"configurazione llm non leggibile: {path}");
        }
    }

    /// Solo i modelli Anthropic vogliono i marcatori cache_control espliciti;
    /// gli altri provider mettono in cache il prefisso da soli.
    public bool SupportsCacheControl(string model) => model.StartsWith("anthropic/", StringComparison.Ordinal);
}
