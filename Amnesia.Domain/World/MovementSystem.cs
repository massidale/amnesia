using Amnesia.Core;

namespace Amnesia.World;

/// Trasforma i minuti di gioco trascorsi in cambi di posizione. Camminano tutti
/// alla stessa velocita', cosi' la distanza costa uguale al giocatore e a chi lui
/// insegue; e il movimento e' funzione dei minuti, mai dei fotogrammi, cosi' una
/// macchina lenta non gioca a un gioco diverso.
public sealed class MovementSystem
{
    public const int WalkCellsPerMinute = 16;
    public const string Player = "player";

    /// Il narratore non ha piedi.
    private const string SystemActor = "system";

    private readonly VillageMap _map;
    private readonly Navigator _nav;
    private readonly RoutineTable _routines;

    public MovementSystem(VillageMap map, Navigator navigator, RoutineTable routines)
    {
        _map = map;
        _nav = navigator;
        _routines = routines;
    }

    /// Un luogo che non esiste non e' sopravvivibile in silenzio: il navigatore
    /// rifiuterebbe ogni percorso e il personaggio resterebbe li' tutto il
    /// pomeriggio con l'incarico ancora addosso. Un refuso in un file di
    /// contenuto non e' una mossa legittima che va storta, e' un errore di
    /// programma, e deve leggersi come tale.
    public void SetIntent(WorldState world, string npcId, string place)
    {
        if (!_map.HasPlace(place))
        {
            throw new ArgumentException($"destinazione sconosciuta: {place}", nameof(place));
        }
        world.MovementIntents[npcId] = place;
    }

    /// Un incarico lo chiude chi l'ha dato, mai l'esserci arrivati: arrivare e' lo
    /// spunto di una scena, ed e' la scena, non la camminata, a decidere quando il
    /// personaggio e' libero di tornare alla sua routine.
    public void ClearIntent(WorldState world, string npcId) => world.MovementIntents.Remove(npcId);

    public string IntentOf(WorldState world, string npcId) =>
        world.MovementIntents.TryGetValue(npcId, out var place) ? place : "";

    public void Advance(WorldState world, double minutes)
    {
        // Il tempo non torna indietro, e una fetta negativa metterebbe da parte un
        // resto negativo che si mangia la prossima fetta buona — lo sbaglio di
        // aritmetica di un chiamante verrebbe fuori come un personaggio che
        // singhiozza qualche minuto dopo e da tutt'altra parte.
        if (minutes <= 0.0)
        {
            return;
        }
        foreach (var actorId in world.Actors.Keys.ToList())
        {
            if (actorId == Player || actorId == SystemActor)
            {
                continue;
            }
            if (!Presence.HasPosition(world, actorId))
            {
                continue;
            }
            AdvanceOne(world, actorId, minutes);
        }
    }

    private void AdvanceOne(WorldState world, string npcId, double minutes)
    {
        // Un incarico batte la routine e continua a batterla dopo che il
        // personaggio e' arrivato: la routine non deve riprendersi chi e' stato
        // mandato da qualche parte.
        var destination = IntentOf(world, npcId);
        if (destination.Length == 0)
        {
            destination = _routines.PlaceFor(npcId, world.Minute);
        }
        if (destination.Length == 0)
        {
            return;
        }
        if (Presence.PositionOf(world, npcId) is not Cell here)
        {
            return;
        }
        if (_map.PlaceAt(here) == destination)
        {
            return;
        }

        // Il resto di un passo frazionario e' stato del mondo come tutto il resto:
        // deve sopravvivere a un salvataggio, e non deve colare da un mondo a un
        // altro guidati dallo stesso MovementSystem.
        world.MovementCarry.TryGetValue(npcId, out var carried);
        var budget = carried + (minutes * WalkCellsPerMinute);
        var steps = (int)budget;
        world.MovementCarry[npcId] = budget - steps;
        if (steps <= 0)
        {
            return;
        }
        if (_map.CenterOf(destination) is not Cell target)
        {
            return;
        }

        // Una cella per volta, richiedendo il percorso al navigatore dopo ognuna.
        // Fra due celle ci sono molti percorsi ugualmente corti, quindi prendere
        // sedici celle da un percorso non e' la stessa camminata che prendere due
        // celle da otto percorsi successivi, e gli stessi due minuti lascerebbero
        // il personaggio su celle diverse a seconda di quanto spesso siamo stati
        // chiamati. La cella successiva di un percorso minimo e' funzione pura
        // della cella su cui si sta, percio' una camminata fatta una cella per
        // volta e' la stessa camminata comunque la si affetti. E ci ferma anche
        // sulla soglia della stanza invece di portarci diverse celle piu' in la'.
        var landing = here;
        for (var step = 0; step < steps; step++)
        {
            var route = _nav.Path(landing, target);
            if (route.Count == 0)
            {
                break;
            }
            landing = route[0];
            // Arrivare chiude la camminata per questa fetta: le celle avanzate non
            // si spendono ne' qui ne' dopo, cosi' nessuno supera la porta che
            // voleva.
            if (_map.PlaceAt(landing) == destination)
            {
                break;
            }
        }
        if (landing == here)
        {
            return;
        }
        Presence.SetPosition(world, npcId, landing);
    }
}
