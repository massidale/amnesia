using Amnesia.Core;

namespace Amnesia.Knowledge;

/// L'isolamento epistemico. Ogni personaggio ha la sua colonna e nessuno legge
/// quella di un altro: e' il meccanismo per cui un modello non puo' rivelare una
/// cosa prima del tempo — non si trattiene, semplicemente non ce l'ha.
public sealed class KnowledgeService
{
    private readonly WorldState _state;

    public KnowledgeService(WorldState state) => _state = state;

    private Dictionary<string, Belief> Bucket(string actorId)
    {
        if (!_state.Knowledge.TryGetValue(actorId, out var bucket))
        {
            bucket = new Dictionary<string, Belief>();
            _state.Knowledge[actorId] = bucket;
        }
        return bucket;
    }

    public void RevealFact(string actorId, string factId, string proposition, string sourceId, double confidence)
    {
        var bucket = Bucket(actorId);
        // Rivedere una convinzione non la sposta in fondo alla fila: il personaggio
        // ha corretto una cosa che sapeva gia', non ne ha imparata una nuova.
        var sequence = bucket.TryGetValue(factId, out var existing) ? existing.Sequence : bucket.Count;
        bucket[factId] = Belief.Create(proposition, sourceId, confidence, sequence);
    }

    /// Cio' che il giocatore afferma non diventa vero: diventa una cosa che
    /// QUESTO personaggio ha sentito dire, con lui come fonte. Puo' crederci,
    /// dubitarne o respingerla, ma resta un'affermazione altrui.
    public void RecordClaim(string listenerId, string speakerId, string factId, string proposition, double confidence) =>
        RevealFact(listenerId, factId, proposition, speakerId, confidence);

    public bool Knows(string actorId, string factId) => Bucket(actorId).ContainsKey(factId);

    /// In ordine di acquisizione, sempre. Un Dictionary in .NET non promette
    /// nessun ordine, e queste righe finiscono nel prompt: lasciarlo al caso vuol
    /// dire byte diversi a parita' di stato, cioe' cache mancata e prompt che
    /// cambiano senza che nessuno li abbia cambiati.
    public IReadOnlyList<Belief> ContextFor(string actorId) =>
        Bucket(actorId).Values.OrderBy(belief => belief.Sequence).ToList();
}
