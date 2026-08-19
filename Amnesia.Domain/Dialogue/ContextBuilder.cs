using System.Globalization;
using Amnesia.Core;
using Amnesia.Knowledge;

namespace Amnesia.Dialogue;

/// Il prompt di un turno, in due pezzi: un prefisso che non cambia mai e una coda
/// che cambia sempre. La divisione non e' estetica, e' il conto della bolletta.
public sealed class ContextBuilder
{
    private readonly string _rulesText;
    private readonly IReadOnlyDictionary<string, string> _characterTexts;
    private readonly ItemCatalog _items;
    private readonly bool _useCacheControl;
    private readonly DeclarationTable? _declarations;
    private readonly PositionTable? _positions;

    /// Il catalogo sta qui insieme alle regole e alle schede perche' e' la stessa
    /// materia: testo d'autore che vale per tutta la partita, non stato che cambia
    /// da un turno all'altro.
    /// Le due tabelle sono facoltative: un personaggio senza scala di posizione
    /// non perde niente, e un chiamante che non le ha costruisce lo stesso prompt
    /// di prima.
    public ContextBuilder(
        string rules,
        IReadOnlyDictionary<string, string> characters,
        ItemCatalog items,
        bool useCacheControl,
        DeclarationTable? declarations = null,
        PositionTable? positions = null)
    {
        _rulesText = rules;
        _characterTexts = characters;
        _items = items;
        _useCacheControl = useCacheControl;
        _declarations = declarations;
        _positions = positions;
    }

    /// Identico byte per byte a ogni turno: qui dentro non puo' colare niente di
    /// dinamico, o la cache del fornitore manca a ogni singola richiesta (design §13).
    public IReadOnlyList<PromptMessage> StaticPrefix(string npcId)
    {
        var character = _characterTexts.TryGetValue(npcId, out var sheet) ? sheet : "";
        if (_useCacheControl)
        {
            return new[]
            {
                new PromptMessage(ChatRole.System, new[]
                {
                    new PromptPart(_rulesText),
                    // Il breakpoint sta in fondo all'ultima parte statica: tutto
                    // cio' che lo precede e' riusabile a ogni turno.
                    new PromptPart(character, CacheBreakpoint: true),
                }),
            };
        }
        return new[] { PromptMessage.Plain(ChatRole.System, _rulesText + "\n\n" + character) };
    }

    public IReadOnlyList<PromptMessage> Build(
        string npcId,
        WorldState world,
        IEnumerable<LoggedMessage> history,
        TurnContext turn)
    {
        var messages = new List<PromptMessage>(StaticPrefix(npcId));
        foreach (var past in history)
        {
            messages.Add(PromptMessage.Plain(past.Role, past.Content));
        }
        messages.Add(PromptMessage.Plain(ChatRole.User, DynamicTail(npcId, world, turn)));
        return messages;
    }

    /// Lo stato dinamico vive SOLO qui, dopo il prefisso in cache (design §13).
    private string DynamicTail(string npcId, WorldState world, TurnContext turn)
    {
        var parts = new List<string>
        {
            $"<stato_mondo>ora: {turn.ClockText}</stato_mondo>",
        };
        var stance = PositionLines(npcId, world);
        if (stance.Length > 0)
        {
            parts.Add($"<posizione>{stance}</posizione>");
        }
        parts.Add($"<conoscenze>{KnowledgeLines(npcId, world)}</conoscenze>");
        foreach (var note in turn.NpcNotes)
        {
            // SICUREZZA: una nota la scrive il motore oggi, ma e' stato del mondo —
            // sopravvive a un salvataggio — quindi si neutralizza come ogni altra
            // stringa che entra nel prompt.
            parts.Add($"<accaduto_di_recente>{PlayerInput.Sanitize(note)}</accaduto_di_recente>");
        }
        foreach (var itemId in turn.ShownItemIds)
        {
            // SICUREZZA: le descrizioni degli oggetti sono d'autore, ma restano
            // stringhe del mondo lo stesso — si neutralizzano perche' nessun
            // percorso di dati possa mai forgiare un blocco qui.
            parts.Add($"<osservazione_motore>Il giocatore mostra: {PlayerInput.Sanitize(_items.VisibleOf(itemId))}</osservazione_motore>");
        }
        // Il motore constata: quelle due righe SONO state dette, e lui lo sa. Non
        // sono parole del giocatore, e un modello che nega un'accusa non puo'
        // comunque disdirle. Il motore cita, quindi non cita cio' di cui non ha il
        // testo: senza le tabelle, o senza una delle due righe, il blocco non c'e'.
        if (turn.Confronto is { } pair && _declarations is not null)
        {
            var first = _declarations.TextOf(pair.First);
            var second = _declarations.TextOf(pair.Second);
            if (first.Length > 0 && second.Length > 0)
            {
                // SICUREZZA: testo autoriale, neutralizzato come ogni altra stringa
                // che entra nel prompt.
                parts.Add(
                    "<osservazione_motore>Il giocatore ti mette davanti due cose che sono state dette: "
                    + $"«{PlayerInput.Sanitize(first)}» e «{PlayerInput.Sanitize(second)}».</osservazione_motore>");
            }
        }
        // SICUREZZA: le parole del giocatore non sono fidate — si neutralizzano
        // perche' non possano forgiare blocchi.
        parts.Add($"<parole_giocatore>{PlayerInput.Sanitize(turn.Spoken)}</parole_giocatore>");
        return string.Join("\n", parts);
    }

    /// Cio' che questo personaggio, oggi, ritiene di poter dire — in prima persona
    /// e come convinzione sincera. Mai una scaletta: il nome del gradino non entra
    /// qui, e non c'e' nessuna istruzione a tacere. Cio' che non deve uscire
    /// semplicemente non compare, perche' un modello non trapela quello che non ha.
    /// SECURITY: testo autoriale, ma passa dal prompt come tutto il resto —
    /// sanitizzato per la stessa ragione delle conoscenze.
    private string PositionLines(string npcId, WorldState world)
    {
        if (_declarations is null || _positions is null)
        {
            return "";
        }
        var lines = new List<string>();
        foreach (var declarationId in _positions.Granted(npcId, world))
        {
            var text = _declarations.TextOf(declarationId);
            if (text.Length > 0)
            {
                lines.Add(PlayerInput.Sanitize(text));
            }
        }
        return string.Join(" ", lines);
    }

    /// SICUREZZA: la conoscenza non e' testo d'autore — RecordClaim lascia che il
    /// modello scriva le parole del giocatore dentro lo stato del mondo, quindi una
    /// dichiarazione registrata e' un percorso di riciclaggio che riporta quelle
    /// parole nel prompt (parole del giocatore → RecordClaim → conoscenza → turno
    /// dopo). Si neutralizza ogni stringa interpolata, proposizione e fonte allo
    /// stesso modo, perche' nessuna dichiarazione registrata possa forgiare un
    /// blocco del motore.
    private static string KnowledgeLines(string npcId, WorldState world)
    {
        var lines = new KnowledgeService(world).ContextFor(npcId).Select(belief => string.Format(
            // La cultura della macchina non decide i byte del prompt: con una
            // virgola decimale al posto del punto ogni prompt validato sarebbe un
            // altro prompt, a seconda di dove gira il gioco.
            CultureInfo.InvariantCulture,
            "{0} (fonte: {1}, confidenza: {2:0.0})",
            PlayerInput.Sanitize(belief.Proposition),
            PlayerInput.Sanitize(belief.SourceId),
            belief.Confidence));
        return string.Join("; ", lines);
    }
}
