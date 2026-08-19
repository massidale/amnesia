using Amnesia.Core;

namespace Amnesia.World;

/// Dove sono gli attori. La posizione vive dentro l'attore in WorldState, cosi'
/// si serializza, si salva e torna indietro insieme a tutto il resto.
///
/// Un attore senza posizione non e' "da nessuna parte": e' "dove serve che sia".
/// Le interfacce testuali non hanno mappa, e una catena autoriale che li' smette
/// di scattare in silenzio e' una regressione travestita da funzionalita'.
public static class Presence
{
    public static bool HasPosition(WorldState world, string actorId) =>
        world.Actors.TryGetValue(actorId, out var actor) && actor.Position is not null;

    /// Niente per chi non ha posizione. In GDScript rispondeva Vector2i.ZERO, che
    /// e' una cella vera in cima alla mappa: il tipo dice ora quello che li'
    /// bisognava ricordarsi di chiedere prima.
    public static Cell? PositionOf(WorldState world, string actorId) =>
        world.Actors.TryGetValue(actorId, out var actor) ? actor.Position : null;

    public static void SetPosition(WorldState world, string actorId, Cell cell) =>
        world.ActorOf(actorId).Position = cell;

    public static string PlaceOf(WorldState world, string actorId, VillageMap map) =>
        PositionOf(world, actorId) is Cell cell ? map.PlaceAt(cell) : "";

    /// Due attori sono insieme quando stanno nello stesso luogo con un nome, non
    /// sulla stessa cella: il sistema di movimento si ferma sulla prima cella
    /// della stanza che incontra, praticamente mai quella su cui sta un altro.
    /// Una soglia non appartiene a nessuna stanza, quindi due attori su due
    /// soglie non sono insieme — un luogo vuoto non e' un luogo.
    public static bool CoLocated(WorldState world, string a, string b, VillageMap map)
    {
        if (!HasPosition(world, a) || !HasPosition(world, b))
        {
            return true;
        }
        var place = PlaceOf(world, a, map);
        if (place.Length == 0)
        {
            return false;
        }
        return place == PlaceOf(world, b, map);
    }
}
