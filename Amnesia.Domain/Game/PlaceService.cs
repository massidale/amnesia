using Amnesia.Core;

namespace Amnesia.Game;

/// Cosa si e' trovato aprendo: la riga da leggere e la roba che e' cambiata di
/// mano. Gli oggetti sono id, non frasi: la frase la scrive la vista, la roba
/// la sposta il motore.
public sealed record Apertura(string Racconto, IReadOnlyList<string> Presi);

/// Le porte. Aprire e' l'unica cosa che in questo gioco non si fa parlando, ed
/// e' per questo che le serrature sono poche e tutte fisiche: quello che si
/// chiude e' un posto, mai un discorso.
///
/// Una porta si apre con due cose insieme — un ferro in tasca e un nome che
/// qualcuno ti ha detto. Il ferro Giorgio ce l'ha dal primo minuto: quello che
/// gli manca, per un atto intero, e' sapere dove.
public sealed class PlaceService
{
    private readonly PlaceTable _luoghi;

    public PlaceService(PlaceTable luoghi) => _luoghi = luoghi;

    public bool IsOpen(WorldState world, string placeId) =>
        world.Flags.TryGetValue(Chiave(placeId), out var aperto) && aperto;

    /// Cosa si vede da fuori. Serve a scriverlo sullo schermo quando ci si
    /// avvicina: la porta si deve vedere prima di poterla aprire, o non e' una
    /// serratura onesta.
    public string Descrizione(string placeId) => _luoghi.Find(placeId)?.Closed ?? "";

    public Result<Apertura> Apri(WorldState world, string placeId, string playerId = "player", bool raccogliContenuto = true)
    {
        var luogo = _luoghi.Find(placeId);
        if (luogo is null)
        {
            return Result<Apertura>.Fail("no_place", "qui non c'e' niente da aprire");
        }
        if (IsOpen(world, placeId))
        {
            return Result<Apertura>.Fail("already_open", "e' gia' aperto");
        }
        if (luogo.RequiresItem.Length > 0
            && (!world.ItemOwners.TryGetValue(luogo.RequiresItem, out var chi) || chi != playerId))
        {
            return Result<Apertura>.Fail("needs_item", luogo.RequiresItem);
        }
        // Basta che qualcuno l'abbia detta: sapere dove sta una porta non e' una
        // cosa da dimostrare con due testimoni.
        foreach (var detta in luogo.RequiresDeclared)
        {
            if (!world.Declarations.TryGetValue(detta, out var riga) || riga.Supports.Count == 0)
            {
                return Result<Apertura>.Fail("needs_knowledge", detta);
            }
        }

        world.Flags[Chiave(placeId)] = true;
        var presi = new List<string>();
        foreach (var oggetto in raccogliContenuto ? luogo.Contains : Enumerable.Empty<string>())
        {
            // Quello che qualcun altro ha gia' preso non ricompare: il mondo e'
            // uno solo, e la roba sta in un posto per volta.
            if (!world.ItemOwners.ContainsKey(oggetto))
            {
                world.ItemOwners[oggetto] = playerId;
                presi.Add(oggetto);
            }
        }
        return Result<Apertura>.Ok(new Apertura(luogo.Opened, presi));
    }

    public Result<string> Raccogli(WorldState world, string placeId, string itemId, string playerId = "player")
    {
        var luogo = _luoghi.Find(placeId);
        if (luogo is null || !luogo.Contains.Contains(itemId))
            return Result<string>.Fail("no_item", itemId);
        if (!IsOpen(world, placeId)) return Result<string>.Fail("closed", placeId);
        if (world.ItemOwners.ContainsKey(itemId)) return Result<string>.Fail("already_owned", itemId);
        world.ItemOwners[itemId] = playerId;
        return Result<string>.Ok(itemId);
    }

    private static string Chiave(string placeId) => $"aperto:{placeId}";
}
