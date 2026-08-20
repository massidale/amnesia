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
    private readonly ReactionTable _reactions;

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
        PositionTable? positions = null,
        ReactionTable? reactions = null)
    {
        _rulesText = rules;
        _characterTexts = characters;
        _items = items;
        _useCacheControl = useCacheControl;
        _declarations = declarations;
        _positions = positions;
        _reactions = reactions ?? ReactionTable.Empty();
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
        var parts = new List<string>();
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
            parts.Add($"<osservazione_motore>{PlayerInput.Sanitize(note)}</osservazione_motore>");
        }
        var gradini = _positions?.ReachedIds(npcId, world) ?? Array.Empty<string>();
        // L'autorizzazione a sforare non ha un canale suo: si cuce in coda al
        // primo copione del turno, che e' dove il modello sta gia' guardando.
        var codaDiSvolta = turn.Svolta
            ? " Quello che e' appena successo cambia le cose: racconta per esteso, con calma — stavolta puoi superare le sei frasi."
            : "";
        foreach (var itemId in turn.ShownItemIds)
        {
            // SICUREZZA: le descrizioni degli oggetti sono d'autore, ma restano
            // stringhe del mondo lo stesso — si neutralizzano perche' nessun
            // percorso di dati possa mai forgiare un blocco qui.
            // L'oggetto e' SUO: la demo ha prodotto un personaggio convinto che
            // la cosa mostrata fosse di sua proprieta', e il motore adesso lo
            // dice ogni volta.
            parts.Add("<osservazione_motore>Il giocatore ti mostra una cosa SUA, che tiene in mano lui: "
                + PlayerInput.Sanitize(_items.VisibleOf(itemId))
                + " L'oggetto appartiene a lui e se lo riporta via lui.</osservazione_motore>");
            parts.Add(ComeReagisci(npcId, gradini, itemId, codaDiSvolta));
            codaDiSvolta = "";
        }
        if (turn.FraseDetta)
        {
            var reazione = _reactions.Reazione(npcId, gradini, "frase");
            if (reazione.Length > 0)
            {
                parts.Add($"<come_reagisci>{PlayerInput.Sanitize(reazione)}{codaDiSvolta}</come_reagisci>");
                codaDiSvolta = "";
            }
        }
        if (codaDiSvolta.Length > 0)
        {
            // Svolta senza un copione a cui cucirla: il blocco nasce solo per lei.
            parts.Add($"<come_reagisci>{codaDiSvolta.TrimStart()}</come_reagisci>");
        }
        // SICUREZZA: le parole del giocatore non sono fidate — si neutralizzano
        // perche' non possano forgiare blocchi.
        parts.Add($"<parole_giocatore>{PlayerInput.Sanitize(turn.Spoken)}</parole_giocatore>");
        return string.Join("\n", parts);
    }

    /// Il copione per l'oggetto sul banco. Se nessuno ha scritto una voce, il
    /// ripiego e' la regola di ferro: questa cosa non la conosci, dillo.
    private string ComeReagisci(string npcId, IReadOnlyList<string> gradini, string itemId, string coda = "")
    {
        var reazione = _reactions.Reazione(npcId, gradini, itemId);
        if (reazione.Length == 0)
        {
            reazione = "Questo oggetto non ti dice niente. Dillo apertamente, con parole tue, "
                + "senza inventare, senza dedurre e senza fare nomi.";
        }
        // SICUREZZA: testo d'autore, ma passa dal prompt come tutto il resto.
        return $"<come_reagisci>{PlayerInput.Sanitize(reazione)}{coda}</come_reagisci>";
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
        // Chi non ha una scala non e' uno a cui manca qualcosa: e' uno che non
        // nasconde niente, e le righe di cui e' fonte deve averle davanti. Un
        // paesano che non ha il testo della versione del paese la racconta a
        // modo suo — e il coro, che e' la prova migliore del gioco, non esiste.
        foreach (var declarationId in DeclarationService.Sayable(_declarations, _positions, npcId, world))
        {
            var text = _declarations.TextOf(declarationId, npcId);
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
