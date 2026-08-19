using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;

namespace Amnesia.World;

/// Da quale minuto un personaggio e' atteso in un luogo.
public sealed class RoutineEntry
{
    [JsonPropertyName("from_minute")]
    public int FromMinute { get; set; }

    [JsonPropertyName("place")]
    public string Place { get; set; } = "";
}

/// Dove ogni personaggio dovrebbe stare a una certa ora. Dati, non codice: il
/// ritmo della giornata del paese e' contenuto e sta insieme al contenuto.
public sealed class RoutineTable
{
    public const string DefaultPath = "content/routines.json";

    public IReadOnlyDictionary<string, IReadOnlyList<RoutineEntry>> Entries { get; }

    private RoutineTable(IReadOnlyDictionary<string, IReadOnlyList<RoutineEntry>> entries) =>
        Entries = entries;

    public static RoutineTable Empty() =>
        new(new Dictionary<string, IReadOnlyList<RoutineEntry>>());

    public static Result<RoutineTable> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<RoutineTable>.Fail("routines_not_found", $"routine non leggibili: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<RoutineTable> FromJson(string json)
    {
        Dictionary<string, List<RoutineEntry>>? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dictionary<string, List<RoutineEntry>>>(json, Dto.Options);
        }
        catch (JsonException)
        {
            return Result<RoutineTable>.Fail("invalid_routines", "routine illeggibili");
        }
        if (dto is null)
        {
            return Result<RoutineTable>.Fail("invalid_routines", "routine illeggibili");
        }

        var entries = new Dictionary<string, IReadOnlyList<RoutineEntry>>();
        foreach (var entry in dto)
        {
            entries[entry.Key] = entry.Value;
        }
        return Result<RoutineTable>.Ok(new RoutineTable(entries));
    }

    /// L'ultima voce gia' scattata vince, nell'ordine in cui il file le elenca.
    /// Prima della prima voce non c'e' nessun luogo atteso: un personaggio senza
    /// routine per quell'ora non e' un personaggio in mezzo alla strada.
    public string PlaceFor(string npcId, int minute)
    {
        if (!Entries.TryGetValue(npcId, out var schedule))
        {
            return "";
        }
        var place = "";
        foreach (var entry in schedule)
        {
            if (minute >= entry.FromMinute)
            {
                place = entry.Place;
            }
        }
        return place;
    }
}
