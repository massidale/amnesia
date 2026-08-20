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

    /// Tutti mostrati, in AND. Un oggetto che manca tiene giu' il gradino.
    [JsonPropertyName("requires_shown")]
    public List<string> RequiresShown { get; set; } = new();

    /// Almeno uno mostrato, in OR. E' la forma delle serrature a piu' chiavi:
    /// la scatola dei vestiti OPPURE la cartella clinica portano tutte e due allo
    /// stesso posto, e un gioco d'indagine con una sola strada e' un corridoio.
    [JsonPropertyName("requires_any_shown")]
    public List<string> RequiresAnyShown { get; set; } = new();

    /// Proposizioni che devono risultare STABILITE nel registro — due sostegni
    /// indipendenti, non una che qualcuno ha detto una volta.
    ///
    /// Serve perche' certi gradini non si aprono con un oggetto ma con una cosa
    /// che il gioco ha accertato: la busta del padre esce quando Elena risulta
    /// viva, non quando qualcuno ha mostrato al prete un foglio con un indirizzo.
    [JsonPropertyName("requires_declared")]
    public List<string> RequiresDeclared { get; set; } = new();

    /// Gli oggetti che passano di mano quando questo gradino viene raggiunto.
    /// E' l'unico canale con cui un personaggio DA' qualcosa al giocatore: il
    /// modello puo' raccontare la consegna come vuole, ma la merce la muove il
    /// motore, e solo per questa riga di dati. Stessa separazione di sempre fra
    /// le parole e la roba.
    [JsonPropertyName("consegna")]
    public List<string> Consegna { get; set; } = new();

    /// SICUREZZA DEI DATI: System.Text.Json ignora in silenzio le chiavi che non
    /// conosce, quindi un autore che scrive "requires_declarated" per errore non
    /// vede niente — nessun errore, e un cancello che semplicemente non c'e'.
    /// Qui le raccogliamo, e il caricatore rifiuta il file.
    [JsonExtensionData]
    public Dictionary<string, object>? Unknown { get; set; }
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
            foreach (var step in entry.Value)
            {
                if (step.Unknown is { Count: > 0 })
                {
                    return Result<PositionTable>.Fail("unknown_position_field",
                        $"{entry.Key}/{step.Id}: campo sconosciuto \"{step.Unknown.Keys.First()}\" — un refuso qui e' un cancello che non esiste");
                }
            }
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
    /// Tutto quello che la scala concede, dal primo gradino all'ultimo,
    /// indipendentemente da dove si e' arrivati.
    public IReadOnlyList<string> AllGrants(string npcId) =>
        _ladders.TryGetValue(npcId, out var scala)
            ? scala.SelectMany(gradino => gradino.Grants).ToList()
            : (IReadOnlyList<string>)Array.Empty<string>();

    /// Gli id dei gradini gia' saliti, dal primo all'ultimo. Il copione delle
    /// reazioni li scorre dall'alto per trovare la voce piu' recente.
    public IReadOnlyList<string> ReachedIds(string npcId, WorldState world)
    {
        var ids = new List<string>();
        foreach (var step in Reached(npcId, world))
        {
            ids.Add(step.Id);
        }
        return ids;
    }

    /// Gli oggetti che i gradini gia' raggiunti di questo personaggio hanno da
    /// consegnare. Il chiamante decide se sono gia' passati di mano.
    public IReadOnlyList<string> ConsegnateFinora(string npcId, WorldState world)
    {
        var oggetti = new List<string>();
        foreach (var step in Reached(npcId, world))
        {
            foreach (var itemId in step.Consegna)
            {
                if (!oggetti.Contains(itemId))
                {
                    oggetti.Add(itemId);
                }
            }
        }
        return oggetti;
    }

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
        var register = new Register(world);
        var reached = new List<PositionStep>();
        foreach (var step in ladder)
        {
            if (!Satisfied(step, world, npcId, shown, register))
            {
                break;
            }
            reached.Add(step);
        }
        return reached;
    }

    private static bool Satisfied(
        PositionStep step, WorldState world, string npcId, IReadOnlyList<string> shown, Register register)
    {
        if (step.RequiresShown.Any(itemId => !shown.Contains(itemId)))
        {
            return false;
        }
        if (step.RequiresAnyShown.Count > 0 && !step.RequiresAnyShown.Any(shown.Contains))
        {
            return false;
        }
        return !step.RequiresDeclared.Any(id => !register.IsEstablished(id));
    }

    private sealed class TableDto
    {
        [JsonPropertyName("positions")]
        public Dictionary<string, List<PositionStep>> Positions { get; set; } = new();
    }
}
