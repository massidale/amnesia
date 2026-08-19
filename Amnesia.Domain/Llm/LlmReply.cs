using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amnesia.Llm;

/// Cio' che resta di una risposta del provider dopo averla validata: il testo,
/// le chiamate a strumento e il conto dei token.
public sealed record LlmReply
{
    public string Text { get; init; } = "";

    public IReadOnlyList<ToolCall> ToolCalls { get; init; } = Array.Empty<ToolCall>();

    public TokenUsage Usage { get; init; } = TokenUsage.Empty;

    public string FinishReason { get; init; } = "";
}

/// Una chiamata a strumento gia' validata: gli argomenti sono certamente un
/// oggetto JSON, cosi' chi la consuma non deve ricontrollarlo.
public sealed record ToolCall
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public JsonElement Arguments { get; init; }

    /// La stringa esatta arrivata dal provider. Rimandare indietro questa invece
    /// di riserializzare Arguments tiene il giro di andata e ritorno fedele byte
    /// per byte, e il prefisso della conversazione resta quello messo in cache.
    public string RawArguments { get; init; } = "";

    public ToolCallWire ToWire() => new()
    {
        Id = Id,
        Function = new FunctionCallWire { Name = Name, Arguments = RawArguments },
    };
}

public sealed record TokenUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; init; }

    public static readonly TokenUsage Empty = new();
}
