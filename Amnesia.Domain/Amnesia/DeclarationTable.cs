using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;
using Amnesia.World;

namespace Amnesia;

/// Una cosa che qualcuno puo' dire, scritta dall'autore: un identificativo, il
/// testo canonico, chi e' in condizione di dirla e a quale condizione.
///
/// Il testo canonico NON e' la battuta. Il modello scrive la sua prosa come gli
/// pare; quando ha davvero detto questa cosa chiama lo strumento a vocabolario
/// chiuso con l'id, e il motore registra. Puoi dire quello che vuoi, ma solo il
/// motore stabilisce che l'hai detto.
public sealed class Declaration
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    /// Serve al motore e alla campagna di validazione, e a nient'altro: non entra
    /// in nessun prompt e nessun personaggio lo legge mai. Su venticinque
    /// dichiarazioni sei o sette sono false, dette da gente che ci crede — e un
    /// personaggio non sa mai di mentire.
    [JsonPropertyName("truth")]
    public string Truth { get; set; } = "";

    [JsonPropertyName("sources")]
    public List<string> Sources { get; set; } = new();

    [JsonPropertyName("requires_shown")]
    public List<string> RequiresShown { get; set; } = new();
}

/// La tabella delle dichiarazioni: contenuto d'autore, non stato di partita.
public sealed class DeclarationTable
{
    public const string DefaultPath = "content/amnesia/declarations.json";

    private readonly IReadOnlyDictionary<string, Declaration> _rows;

    private DeclarationTable(IReadOnlyDictionary<string, Declaration> rows) => _rows = rows;

    public static DeclarationTable Empty() => new(new Dictionary<string, Declaration>());

    public static Result<DeclarationTable> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<DeclarationTable>.Fail("declarations_not_found", $"tabella delle dichiarazioni non leggibile: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<DeclarationTable> FromJson(string json)
    {
        TableDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TableDto>(json, Dto.Options);
        }
        catch (JsonException)
        {
            return Result<DeclarationTable>.Fail("invalid_declarations", "tabella delle dichiarazioni illeggibile");
        }
        if (dto is null)
        {
            return Result<DeclarationTable>.Fail("invalid_declarations", "tabella delle dichiarazioni illeggibile");
        }

        var rows = new Dictionary<string, Declaration>();
        foreach (var entry in dto.Declarations)
        {
            rows[entry.Key] = entry.Value;
        }
        return Result<DeclarationTable>.Ok(new DeclarationTable(rows));
    }

    /// Tutti gli identificativi, per confrontare i dati con il vocabolario chiuso
    /// del catalogo — che e' l'unico posto dove i due possono divergere in
    /// silenzio.
    public IReadOnlyList<string> Ids => _rows.Keys.ToList();

    public bool Has(string id) => _rows.ContainsKey(id);

    /// Un id sconosciuto rende niente invece di rompere: la tabella e' dati, e un
    /// refuso nei dati deve costare una riga mancante, non una partita interrotta.
    public Declaration? Find(string id) => _rows.TryGetValue(id, out var row) ? row : null;

    public string TextOf(string id) => Find(id)?.Text ?? "";

    public IReadOnlyList<string> SourcesOf(string id) =>
        Find(id)?.Sources ?? (IReadOnlyList<string>)Array.Empty<string>();

    public IReadOnlyList<string> RequiresShown(string id) =>
        Find(id)?.RequiresShown ?? (IReadOnlyList<string>)Array.Empty<string>();

    private sealed class TableDto
    {
        [JsonPropertyName("declarations")]
        public Dictionary<string, Declaration> Declarations { get; set; } = new();
    }
}
