using System.Text.Json.Serialization;

namespace Amnesia.Llm;

/// Un blocco di contenuto nella forma a pezzi. Serve a una cosa sola, e vale
/// parecchi soldi: marcare dove il fornitore puo' tagliare il prefisso da
/// riusare fra un turno e l'altro.
public sealed record ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; init; } = "";

    /// Ignorato quando e' nullo, sul tipo e non sulle opzioni del chiamante: un
    /// `"cache_control": null` su ogni blocco e' spazzatura sul filo, e qualche
    /// fornitore la rifiuta invece di ignorarla.
    [JsonPropertyName("cache_control")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CacheControl? CacheControl { get; init; }

    public static ContentBlock Text_(string text, bool breakpoint = false) => new()
    {
        Text = text,
        CacheControl = breakpoint ? new CacheControl() : null,
    };
}

public sealed record CacheControl
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "ephemeral";
}
