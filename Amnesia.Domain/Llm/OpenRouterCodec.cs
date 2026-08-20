using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;

namespace Amnesia.Llm;

/// Il corpo di una richiesta di chat.
public sealed record ChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = "";

    [JsonPropertyName("messages")]
    public IReadOnlyList<ChatMessage> Messages { get; init; } = Array.Empty<ChatMessage>();

    [JsonPropertyName("tools")]
    public IReadOnlyList<ToolDefinition> Tools { get; init; } = Array.Empty<ToolDefinition>();

    /// "auto": il modello decide se invocare uno strumento. Forzarlo lo farebbe
    /// chiamare anche quando la battuta giusta e' solo parlare.
    [JsonPropertyName("tool_choice")]
    public string ToolChoice { get; init; } = "auto";

    /// Le regole chiedono da due a sei frasi. Senza un tetto il modello ogni
    /// tanto scrive un tema, e il giocatore aspetta trenta secondi una risposta
    /// che gli arriva lunga il doppio di quanto dovrebbe essere.
    [JsonPropertyName("max_tokens")]
    /// Abbastanza per le sei righe di un turno normale E per il racconto lungo
    /// di una svolta: il tetto basso di prima (340) tagliava a meta' proprio i
    /// momenti in cui il motore autorizza a sforare.
    public int MaxTokens { get; init; } = 520;

    /// Lo stesso modello, su OpenRouter, gira su piu' fornitori, e la
    /// differenza fra il piu' rapido e il piu' lento e' quasi tutta la latenza
    /// che si sente giocando. Misurato sul prompt vero (5100 token): con la
    /// scelta lasciata al caso, dai 5 agli 11 secondi; scegliendo per latenza,
    /// da 1,6 a 3,6. La generazione, in mezzo, vale meno di due secondi.
    [JsonPropertyName("provider")]
    public ProviderPreferences Provider { get; init; } = new();
}

public sealed record ProviderPreferences
{
    /// I fornitori si nominano invece di ordinarli per velocita', e la ragione
    /// e' il conto: sullo stesso modello il piu' rapido (Friendli) costa
    /// 0,50 $/M contro gli 0,21 di GMICloud, e ordinare per latenza ci finiva
    /// dritto. Misurati sul prompt vero: Baidu 2,2-3,3 s, StreamLake 3,3-3,5 s,
    /// GMICloud 3,5-4,3 s — contro gli 1,5-3,6 di Friendli, che pero' costa il
    /// doppio e tiene la cache a 0,25 $/M invece che a 0,02.
    ///
    /// Un ordine esplicito fa anche la cosa che conta di piu': tiene le battute
    /// sullo stesso fornitore, e cosi' il prefisso del prompt — cinquemila
    /// token di regole e scheda — resta in cache invece di essere rimacinato a
    /// ogni turno.
    [JsonPropertyName("order")]
    public IReadOnlyList<string> Order { get; init; } = new[] { "Baidu", "StreamLake", "GMICloud" };

    /// Se il fornitore piu' veloce cade, si passa al successivo invece di
    /// restituire un errore: e' la differenza fra una battuta che tarda e una
    /// conversazione che si interrompe.
    [JsonPropertyName("allow_fallbacks")]
    public bool AllowFallbacks { get; init; } = true;
}

/// La traduzione fra il dominio e il formato sul filo di OpenRouter, in
/// entrambe le direzioni. Sta tutta qui perche' e' l'unico punto in cui una
/// modifica silenziosa non fallisce a compilazione ma contro il provider.
public static class OpenRouterCodec
{
    /// Un solo insieme di opzioni per andata e ritorno: due configurazioni
    /// diverse darebbero due forme diverse per la stessa cosa.
    /// L'encoder rilassato serve perche' il blocco degli strumenti e' testo del
    /// prompt: con l'escaping di default ogni apostrofo italiano diventa ',
    /// che il modello legge lo stesso ma paga in token.
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static ChatRequest BuildRequest(string model, IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools) =>
        new() { Model = model, Messages = messages, Tools = tools };

    public static string SerializeRequest(ChatRequest request) =>
        JsonSerializer.Serialize(request, SerializerOptions);

    public static Result<LlmReply> ParseResponse(string json)
    {
        ResponseDto? response;
        try
        {
            response = JsonSerializer.Deserialize<ResponseDto>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return Result<LlmReply>.Fail("malformed_response", "la risposta non ha la forma attesa");
        }

        if (response?.Choices is null || response.Choices.Count == 0)
        {
            return Result<LlmReply>.Fail("malformed_response", "choices mancante");
        }

        var choice = response.Choices[0];
        var message = choice.Message;
        var toolCalls = new List<ToolCall>();
        foreach (var raw in message?.ToolCalls ?? new List<ToolCallDto>())
        {
            var parsed = ParseToolCall(raw);
            if (!parsed.IsOk)
            {
                // Una chiamata rotta fa cadere tutta la risposta: eseguirne meta'
                // lascerebbe il mondo a meta' di un turno che nessuno ha deciso.
                return Result<LlmReply>.Fail(parsed.Code, parsed.Message);
            }
            toolCalls.Add(parsed.Value!);
        }

        return Result<LlmReply>.Ok(new LlmReply
        {
            Text = message?.Content ?? "",
            ToolCalls = toolCalls,
            Usage = ReadUsage(response.Usage),
            FinishReason = choice.FinishReason ?? "",
        });
    }

    private static Result<ToolCall> ParseToolCall(ToolCallDto raw)
    {
        var function = raw.Function;
        var name = function?.Name ?? "";
        if (function is null || function.Arguments.ValueKind != JsonValueKind.String)
        {
            return Result<ToolCall>.Fail("malformed_tool_arguments", name);
        }

        var rawArguments = function.Arguments.GetString() ?? "";
        try
        {
            using var document = JsonDocument.Parse(rawArguments);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Result<ToolCall>.Fail("malformed_tool_arguments", name);
            }

            return Result<ToolCall>.Ok(new ToolCall
            {
                Id = raw.Id ?? "",
                Name = name,
                // Clone: il JsonElement non sopravvive alla Dispose del documento.
                Arguments = document.RootElement.Clone(),
                RawArguments = rawArguments,
            });
        }
        catch (JsonException)
        {
            return Result<ToolCall>.Fail("malformed_tool_arguments", name);
        }
    }

    /// Il conto dei token e' una comodita', non un dato di gioco: se il provider
    /// lo manda storto si continua con zero invece di buttare via la risposta.
    private static TokenUsage ReadUsage(JsonElement usage)
    {
        if (usage.ValueKind != JsonValueKind.Object)
        {
            return TokenUsage.Empty;
        }

        try
        {
            return usage.Deserialize<TokenUsage>(SerializerOptions) ?? TokenUsage.Empty;
        }
        catch (JsonException)
        {
            return TokenUsage.Empty;
        }
    }

    private sealed class ResponseDto
    {
        [JsonPropertyName("choices")]
        public List<ChoiceDto>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public JsonElement Usage { get; set; }
    }

    private sealed class ChoiceDto
    {
        [JsonPropertyName("message")]
        public MessageDto? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private sealed class MessageDto
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<ToolCallDto>? ToolCalls { get; set; }
    }

    /// In lettura gli argomenti restano un JsonElement: il provider potrebbe
    /// mandarli come oggetto invece che come stringa, e quel caso va riportato
    /// come argomenti malformati e non come risposta illeggibile.
    private sealed class ToolCallDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("function")]
        public FunctionCallDto? Function { get; set; }
    }

    private sealed class FunctionCallDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public JsonElement Arguments { get; set; }
    }
}
