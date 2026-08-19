using System.Text.Json.Serialization;

namespace Amnesia.Llm;

/// Un messaggio della conversazione nella forma esatta che il provider accetta.
/// Tipizzato invece che costruito a mano un dizionario alla volta: questa e' la
/// forma che va sul filo, e uno scarto qui si scopre solo contro il provider.
public sealed record ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = "";

    /// Il testo, quando il messaggio e' piatto.
    [JsonIgnore]
    public string? Content { get; init; }

    /// Il testo a pezzi, quando serve un breakpoint di cache. Le due forme si
    /// escludono: un messaggio ha o l'una o l'altra.
    [JsonIgnore]
    public IReadOnlyList<ContentBlock>? ContentBlocks { get; init; }

    /// Il fornitore accetta `content` come stringa oppure come lista di blocchi,
    /// e la seconda forma e' l'unica che puo' portare `cache_control`. Un solo
    /// campo `object` invece di due sul filo: System.Text.Json serializza in base
    /// al tipo a runtime, quindi una stringa esce stringa e una lista esce lista.
    ///
    /// Senza questo, il prefisso in cache verrebbe concatenato in un'unica
    /// stringa al confine fra i moduli, il breakpoint sparirebbe, e OGNI
    /// richiesta mancherebbe la cache — senza rompere niente e senza dirlo.
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ContentWire => ContentBlocks is { Count: > 0 } ? ContentBlocks : Content;

    /// Presenti solo sui messaggi dell'assistente che hanno invocato strumenti.
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ToolCallWire>? ToolCalls { get; init; }

    /// Presente solo sui messaggi di ruolo "tool", che rispondono a una chiamata.
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; init; }

    public static ChatMessage SystemPrompt(string content) => new() { Role = "system", Content = content };

    public static ChatMessage SystemPrompt(IReadOnlyList<ContentBlock> blocks) =>
        new() { Role = "system", ContentBlocks = blocks };

    public static ChatMessage User(string content) => new() { Role = "user", Content = content };

    public static ChatMessage Assistant(string? content, IReadOnlyList<ToolCallWire>? toolCalls = null) =>
        new() { Role = "assistant", Content = content, ToolCalls = toolCalls };

    /// L'esito di uno strumento torna al modello come messaggio a se' stante,
    /// legato alla chiamata dal suo id: senza quel legame il modello non sa a
    /// quale delle chiamate del turno stiamo rispondendo.
    public static ChatMessage ToolResult(string toolCallId, string content) =>
        new() { Role = "tool", ToolCallId = toolCallId, Content = content };
}

/// Una chiamata a strumento come la scrive il filo.
public sealed record ToolCallWire
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    [JsonPropertyName("function")]
    public FunctionCallWire Function { get; init; } = new();
}

public sealed record FunctionCallWire
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    /// Gli argomenti viaggiano come stringa JSON dentro il JSON, non come
    /// oggetto: e' una stranezza del formato OpenAI che OpenRouter eredita, e
    /// ricodificarla come oggetto fa rifiutare la richiesta.
    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = "";
}
