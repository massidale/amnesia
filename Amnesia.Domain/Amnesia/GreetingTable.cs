using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;
using Amnesia.World;

namespace Amnesia;

/// Come ciascuno ti accoglie, gradino per gradino. Non e' una battuta di
/// servizio: e' la prima informazione che il giocatore riceve, e dice in che
/// rapporto e' con quella persona senza che nessuno glielo spieghi.
///
/// Vive nei dati come le scale di posizione, e per la stessa ragione: il testo
/// di un uomo che ha appena ammesso di aver portato via una bambina non e' il
/// testo dello stesso uomo la settimana prima, e la differenza non si scrive in
/// un'istruzione al modello — si scrive in due righe diverse.
public sealed class GreetingTable
{
    public const string DefaultPath = "content/amnesia/saluti.json";

    /// La chiave dei personaggi senza scala: chi non ha gradini ha un saluto solo.
    public const string Sempre = "";

    private readonly IReadOnlyDictionary<string, Dictionary<string, string>> _righe;

    private GreetingTable(IReadOnlyDictionary<string, Dictionary<string, string>> righe) => _righe = righe;

    public static GreetingTable Empty() => new(new Dictionary<string, Dictionary<string, string>>());

    public static Result<GreetingTable> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<GreetingTable>.Fail("greetings_not_found", $"tabella dei saluti non leggibile: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<GreetingTable> FromJson(string json)
    {
        TableDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TableDto>(json, Dto.Options);
        }
        catch (JsonException)
        {
            return Result<GreetingTable>.Fail("invalid_greetings", "tabella dei saluti illeggibile");
        }
        if (dto is null)
        {
            return Result<GreetingTable>.Fail("invalid_greetings", "tabella dei saluti illeggibile");
        }
        return Result<GreetingTable>.Ok(new GreetingTable(dto.Greetings));
    }

    public IReadOnlyList<string> Ids => _righe.Keys.ToList();

    /// Il saluto per quel gradino. Se il gradino non ne ha uno si scende a
    /// quello generico, e se non c'e' nemmeno quello non si saluta: un
    /// personaggio senza riga tace, non dice una riga di un altro.
    public string Line(string npcId, string posizione)
    {
        if (!_righe.TryGetValue(npcId, out var scheda))
        {
            return "";
        }
        if (scheda.TryGetValue(posizione, out var riga) && riga.Length > 0)
        {
            return riga;
        }
        return scheda.TryGetValue(Sempre, out var sempre) ? sempre : "";
    }

    private sealed class TableDto
    {
        [JsonPropertyName("greetings")]
        public Dictionary<string, Dictionary<string, string>> Greetings { get; set; } = new();
    }
}
