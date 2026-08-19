using Amnesia.Core;

namespace Amnesia;

/// Cosa una singola dichiarazione ha raccolto: le bocche che l'hanno detta, in
/// ordine di arrivo, e quante volte ciascuna l'ha ripetuta.
public sealed class RegisterEntry
{
    /// Una cosa detta contro il proprio interesse non ha bisogno di conferme.
    /// Sta sulla riga e non nella tabella perche' e' una proprieta' di come quella
    /// frase e' entrata nel mondo: il registro deve poter rispondere da solo,
    /// anche riletto da un salvataggio, senza avere accanto i dati del contenuto.
    [System.Text.Json.Serialization.JsonPropertyName("counts_alone")]
    public bool CountsAlone { get; set; }

    /// Chi l'ha detta, senza duplicati. Due sostegni vuol dire due persone.
    public List<string> Supports { get; set; } = new();

    /// Le ripetizioni, per bocca. Il collante del quarto atto e' una riga che lo
    /// stesso personaggio ripete, e il coro conta quante bocche dicono la stessa
    /// cosa: e' la stessa macchina, e va costruita adesso.
    public Dictionary<string, int> Counts { get; set; } = new();

    /// L'ordine in cui questa dichiarazione e' entrata nel registro. Come per le
    /// convinzioni: un Dictionary in .NET non promette nessun ordine, e un
    /// registro che risponde in ordini diversi a seconda della build non e' un
    /// registro.
    public int Sequence { get; set; }
}

/// Il registro: chi ha dichiarato cosa, e cosa il gioco considera stabilito. Lo
/// scrive solo il motore — un modello puo' dire quello che vuole, ma una cosa
/// risulta detta solo se e' passata di qui.
///
/// Lo stato sta nel mondo, mai nell'istanza: due registri sullo stesso mondo
/// vedono la stessa cosa, e un salvataggio se lo porta dietro intero.
public sealed class Register
{
    public const int SupportsRequired = 2;

    private readonly WorldState _state;

    public Register(WorldState state) => _state = state;

    public void Record(string speakerId, string declarationId, bool countsAlone = false)
    {
        var entry = Entry(declarationId);
        if (countsAlone)
        {
            entry.CountsAlone = true;
        }
        // Una bocca che si ripete resta una bocca, o chiunque si autoconferma
        // dicendo la stessa cosa due volte.
        if (!entry.Supports.Contains(speakerId))
        {
            entry.Supports.Add(speakerId);
        }
        entry.Counts.TryGetValue(speakerId, out var said);
        entry.Counts[speakerId] = said + 1;
    }

    public IReadOnlyList<string> SupportsFor(string declarationId) =>
        _state.Declarations.TryGetValue(declarationId, out var entry)
            ? entry.Supports
            : (IReadOnlyList<string>)Array.Empty<string>();

    /// Due sostegni indipendenti stabiliscono. E' la regola delle due vie
    /// non-mendaci applicata a runtime: il motore conta le bocche, e non c'e'
    /// niente da interpretare.
    public bool IsEstablished(string declarationId)
    {
        var row = _state.Declarations.TryGetValue(declarationId, out var entry) ? entry : null;
        if (row is null)
        {
            return false;
        }
        // Una confessione contro se stessi vale da sola: nessuno la conferma,
        // perche' l'unico altro che potrebbe non ha nessun motivo di farlo.
        var needed = row.CountsAlone ? 1 : SupportsRequired;
        return row.Supports.Count >= needed;
    }

    public int TimesSaid(string speakerId, string declarationId) =>
        _state.Declarations.TryGetValue(declarationId, out var entry)
        && entry.Counts.TryGetValue(speakerId, out var said)
            ? said
            : 0;

    public IReadOnlyList<string> SaidBy(string speakerId) =>
        _state.Declarations
            .Where(pair => pair.Value.Supports.Contains(speakerId))
            .OrderBy(pair => pair.Value.Sequence)
            .Select(pair => pair.Key)
            .ToList();

    /// Solo chi scrive crea la riga. Una domanda al registro non deve lasciare
    /// traccia nel mondo: un id sbagliato letto mille volte non e' mille
    /// dichiarazioni vuote dentro il salvataggio.
    private RegisterEntry Entry(string declarationId)
    {
        if (!_state.Declarations.TryGetValue(declarationId, out var entry))
        {
            entry = new RegisterEntry { Sequence = _state.Declarations.Count };
            _state.Declarations[declarationId] = entry;
        }
        return entry;
    }
}
