using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;
using Amnesia.World;

namespace Amnesia;

/// Un luogo che si apre: una porta, cosa serve per passarla, e cosa c'e'
/// dietro.
///
/// Si chiude l'accesso fisico, mai un argomento di conversazione. Una porta
/// chiusa e' onesta — il giocatore la vede, capisce che gli manca qualcosa e sa
/// cosa sta cercando; un argomento chiuso e' il gioco che gli dice di no mentre
/// finge di essere una persona.
public sealed class PlaceLock
{
    /// La cella della porta: e' li' che si mette il ferro, ed e' li' che il
    /// giocatore preme per entrare.
    public CellDto Door { get; set; } = new();

    /// Cosa si vede da fuori, e cosa si sente aprendo. Testo, non meccanica.
    public string Closed { get; set; } = "";

    public string Opened { get; set; } = "";

    /// L'oggetto che serve avere in mano.
    [JsonPropertyName("requires_item")]
    public string RequiresItem { get; set; } = "";

    /// Cosa bisogna essersi sentiti dire. Basta una bocca, non due: due sostegni
    /// servono a *dimostrare* una cosa, e qui non si dimostra niente — si sa
    /// dove sta una porta perche' qualcuno te l'ha detto.
    [JsonPropertyName("requires_declared")]
    public List<string> RequiresDeclared { get; set; } = new();

    public List<string> Contains { get; set; } = new();

    /// Una botola non e' una porta: sta per terra. Serve a chi la disegna —
    /// una serranda alta due metri in mezzo a una stanza si legge come un muro,
    /// e il giocatore le gira intorno senza capire che si apre.
    [JsonPropertyName("botola")]
    public bool Botola { get; set; }

    /// Una chiave che nessuno legge e' un lucchetto che non scatta e nessuno se
    /// ne accorge fino a partita in corso: un refuso qui deve fermare il
    /// caricamento, non costare una serata.
    [JsonExtensionData]
    public Dictionary<string, object>? Unknown { get; set; }
}

public sealed class CellDto
{
    public int X { get; set; }
    public int Y { get; set; }

    public Cell Cell() => new(X, Y);
}

public sealed class PlaceTable
{
    public const string DefaultPath = "content/amnesia/luoghi.json";

    private readonly IReadOnlyDictionary<string, PlaceLock> _rows;

    private PlaceTable(IReadOnlyDictionary<string, PlaceLock> rows) => _rows = rows;

    public static PlaceTable Empty() => new(new Dictionary<string, PlaceLock>());

    public static Result<PlaceTable> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<PlaceTable>.Fail("places_not_found", $"tabella dei luoghi non leggibile: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<PlaceTable> FromJson(string json)
    {
        TableDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TableDto>(json, Dto.Options);
        }
        catch (JsonException)
        {
            return Result<PlaceTable>.Fail("invalid_places", "tabella dei luoghi illeggibile");
        }
        if (dto is null)
        {
            return Result<PlaceTable>.Fail("invalid_places", "tabella dei luoghi illeggibile");
        }
        foreach (var entry in dto.Places)
        {
            if (entry.Value.Unknown is { Count: > 0 })
            {
                return Result<PlaceTable>.Fail(
                    "invalid_places",
                    $"il luogo {entry.Key} ha una chiave che nessuno legge: {string.Join(", ", entry.Value.Unknown.Keys)}");
            }
        }
        return Result<PlaceTable>.Ok(new PlaceTable(dto.Places));
    }

    public IReadOnlyList<string> Ids => _rows.Keys.ToList();

    public PlaceLock? Find(string placeId) => _rows.TryGetValue(placeId, out var row) ? row : null;

    /// Il luogo la cui porta e' quella cella, se ce n'e' uno.
    public string PlaceAtDoor(Cell cell)
    {
        foreach (var entry in _rows)
        {
            if (entry.Value.Door.X == cell.X && entry.Value.Door.Y == cell.Y)
            {
                return entry.Key;
            }
        }
        return "";
    }

    private sealed class TableDto
    {
        [JsonPropertyName("places")]
        public Dictionary<string, PlaceLock> Places { get; set; } = new();
    }
}
