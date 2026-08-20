using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;

namespace Amnesia.Game;

/// Una riga del taccuino come la scrive Giorgio: quello che, a questo punto,
/// crede vero. Quando la storia la smentisce non sparisce — viene cancellata a
/// penna, e sotto compare la riga nuova: un taccuino vero porta i segni dei
/// propri errori, ed e' cosi' che il giocatore vede la trama muoversi.
public sealed class Convinzione
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// La riga come la leggerebbe il giocatore, in prima persona.
    [JsonPropertyName("testo")]
    public string Testo { get; set; } = "";

    /// Vera dal primo minuto, senza che nessuno la dica: quello che Giorgio sa
    /// al risveglio.
    [JsonPropertyName("iniziale")]
    public bool Iniziale { get; set; }

    /// Le dichiarazioni che la fanno comparire, in OR: basta che una bocca ne
    /// abbia detta una. Il contenuto informativo intero sta nel testo canonico
    /// della dichiarazione, quindi una sola basta.
    [JsonPropertyName("quando")]
    public List<string> Quando { get; set; } = new();

    /// L'id della convinzione che questa smentisce: quando questa compare,
    /// quella viene cancellata a penna.
    [JsonPropertyName("sostituisce")]
    public string Sostituisce { get; set; } = "";

    /// SICUREZZA DEI DATI: un refuso in una chiave non deve sparire in silenzio.
    [JsonExtensionData]
    public Dictionary<string, object>? Unknown { get; set; }
}

/// La tabella delle convinzioni, nell'ordine d'autore: e' l'ordine in cui le
/// righe compaiono sul taccuino, e non cambia mai.
public sealed class ConvinzioniTable
{
    public const string DefaultPath = "content/amnesia/taccuino.json";

    private readonly IReadOnlyList<Convinzione> _righe;

    private ConvinzioniTable(IReadOnlyList<Convinzione> righe) => _righe = righe;

    public IReadOnlyList<Convinzione> Righe => _righe;

    public static ConvinzioniTable Empty() => new(Array.Empty<Convinzione>());

    public static Result<ConvinzioniTable> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<ConvinzioniTable>.Fail("taccuino_not_found", $"tabella del taccuino non leggibile: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<ConvinzioniTable> FromJson(string json)
    {
        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(json);
        }
        catch (JsonException)
        {
            return Result<ConvinzioniTable>.Fail("invalid_taccuino", "tabella del taccuino illeggibile");
        }
        if (dto?.Convinzioni is null)
        {
            return Result<ConvinzioniTable>.Fail("invalid_taccuino", "tabella del taccuino illeggibile");
        }
        foreach (var riga in dto.Convinzioni)
        {
            if (riga.Unknown is { Count: > 0 })
            {
                return Result<ConvinzioniTable>.Fail("unknown_taccuino_field",
                    $"{riga.Id}: campo sconosciuto \"{riga.Unknown.Keys.First()}\"");
            }
        }
        return Result<ConvinzioniTable>.Ok(new ConvinzioniTable(dto.Convinzioni));
    }

    private sealed class Dto
    {
        [JsonPropertyName("convinzioni")]
        public List<Convinzione>? Convinzioni { get; set; }
    }
}
