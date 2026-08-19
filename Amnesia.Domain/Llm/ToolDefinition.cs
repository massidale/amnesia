using System.Text.Json.Serialization;

namespace Amnesia.Llm;

/// Uno strumento nella forma che OpenRouter vuole: un involucro attorno alla
/// funzione vera.
public sealed record ToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; init; } = new();
}

public sealed record FunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("parameters")]
    public JsonSchema Parameters { get; init; } = new();
}

/// Il frammento di JSON Schema con cui si descrivono gli argomenti di uno
/// strumento. L'ordine dei campi qui e' l'ordine in cui escono sul filo, e
/// quell'ordine e' parte del prefisso messo in cache: vedi ToolCatalog.
public sealed record JsonSchema
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "object";

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("enum")]
    public IReadOnlyList<string>? EnumValues { get; init; }

    [JsonPropertyName("minimum")]
    public double? Minimum { get; init; }

    [JsonPropertyName("maximum")]
    public double? Maximum { get; init; }

    [JsonPropertyName("properties")]
    public IReadOnlyDictionary<string, JsonSchema>? Properties { get; init; }

    [JsonPropertyName("required")]
    public IReadOnlyList<string>? Required { get; init; }
}
