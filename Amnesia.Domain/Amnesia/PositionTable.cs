using System.Text.Json;
using System.Text.Json.Serialization;
using Amnesia.Core;
using Amnesia.World;

namespace Amnesia;

/// Un gradino di una scala di posizione: cosa concede, e cosa gli va messo
/// davanti perche' il personaggio ci salga.
public sealed class PositionStep
{
    /// Il nome del gradino esiste per i test e per il registro di una partita.
    /// Nel prompt non entra mai: a un modello a cui si dice "sei a M1" si sta
    /// dando una scaletta, e lui la recita.
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("grants")]
    public List<string> Grants { get; set; } = new();

    [JsonPropertyName("requires_shown")]
    public List<string> RequiresShown { get; set; } = new();
}

/// Le scale di posizione, una per personaggio guardingo.
///
/// La posizione governa COSA IL PERSONAGGIO HA, non cosa gli e' permesso dire.
/// Un gradino non concede un permesso: aggiunge righe al suo contesto. Un
/// modello non trapela una cosa che non ha, mentre uno a cui si scrive "sai X ma
/// non dirlo" prima o poi lo dice — o peggio, fa la faccia di chi sa.
public sealed class PositionTable
{
    public const string DefaultPath = "content/amnesia/positions.json";

    private readonly IReadOnlyDictionary<string, IReadOnlyList<PositionStep>> _ladders;

    private PositionTable(IReadOnlyDictionary<string, IReadOnlyList<PositionStep>> ladders) =>
        _ladders = ladders;

    public static PositionTable Empty() =>
        new(new Dictionary<string, IReadOnlyList<PositionStep>>());

    public static Result<PositionTable> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<PositionTable>.Fail("positions_not_found", $"tabella delle posizioni non leggibile: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<PositionTable> FromJson(string json)
    {
        TableDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<TableDto>(json, Dto.Options);
        }
        catch (JsonException)
        {
            return Result<PositionTable>.Fail("invalid_positions", "tabella delle posizioni illeggibile");
        }
        if (dto is null)
        {
            return Result<PositionTable>.Fail("invalid_positions", "tabella delle posizioni illeggibile");
        }

        var ladders = new Dictionary<string, IReadOnlyList<PositionStep>>();
        foreach (var entry in dto.Positions)
        {
            ladders[entry.Key] = entry.Value;
        }
        return Result<PositionTable>.Ok(new PositionTable(ladders));
    }

    /// Chi non ha una scala non ha una posizione: non e' un errore, e' un
    /// personaggio che non e' guardingo.
    /// Un personaggio senza scala non e' un errore: e' uno che non ha niente da
    /// tenersi. La differenza decide chi puo' parlare attraverso lo strumento.
    public bool HasLadder(string npcId) => _ladders.ContainsKey(npcId);

    public string PositionOf(string npcId, WorldState world)
    {
        var reached = Reached(npcId, world);
        return reached.Count == 0 ? "" : reached[reached.Count - 1].Id;
    }

    /// Gli id delle dichiarazioni che questo personaggio POSSIEDE adesso.
    public IReadOnlyList<string> Granted(string npcId, WorldState world)
    {
        var granted = new List<string>();
        foreach (var step in Reached(npcId, world))
        {
            foreach (var declarationId in step.Grants)
            {
                if (!granted.Contains(declarationId))
                {
                    granted.Add(declarationId);
                }
            }
        }
        return granted;
    }

    /// I gradini si sommano e si percorrono in ordine: si sale finche' il
    /// prossimo e' soddisfatto, e ci si ferma al primo che non lo e'. Un gradino
    /// saltato tiene giu' tutti quelli sopra, che e' quello che vuole la storia.
    private IReadOnlyList<PositionStep> Reached(string npcId, WorldState world)
    {
        if (!_ladders.TryGetValue(npcId, out var ladder))
        {
            return Array.Empty<PositionStep>();
        }
        var shown = world.ShownToNpc(npcId);
        var reached = new List<PositionStep>();
        foreach (var step in ladder)
        {
            if (step.RequiresShown.Any(itemId => !shown.Contains(itemId)))
            {
                break;
            }
            reached.Add(step);
        }
        return reached;
    }

    private sealed class TableDto
    {
        [JsonPropertyName("positions")]
        public Dictionary<string, List<PositionStep>> Positions { get; set; } = new();
    }
}
