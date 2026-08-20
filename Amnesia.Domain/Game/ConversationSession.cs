using System.Linq;
using System.Diagnostics;
using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Llm;
using Amnesia.Time;

namespace Amnesia.Game;

/// Un turno di conversazione, dall'inizio alla fine: le parole del giocatore, il
/// prompt, il modello, gli strumenti, l'orologio.
///
/// **Il turno e' atomico.** Tutto avviene su una copia del mondo, e la copia
/// prende il posto dell'originale solo se il turno arriva in fondo. Se la rete
/// cade — e cade — il giocatore non si ritrova con un oggetto gia' mostrato, un
/// minuto gia' speso e nessuna risposta: si ritrova esattamente dov'era.
public sealed class ConversationSession
{
    private const int HistoryWindow = 12;

    private readonly ContextBuilder _context;
    private readonly IChatTransport _transport;
    private readonly DeclarationService _declarations;
    private readonly ItemCatalog _items;
    private readonly string _model;
    private readonly string _playerId;

    public WorldState World { get; private set; }
    public ConversationLog Log { get; }

    public ConversationSession(
        WorldState world,
        ConversationLog log,
        ContextBuilder context,
        IChatTransport transport,
        DeclarationService declarations,
        ItemCatalog items,
        string model,
        string playerId = "player")
    {
        World = world;
        Log = log;
        _context = context;
        _transport = transport;
        _declarations = declarations;
        _items = items;
        _model = model;
        _playerId = playerId;
    }

    /// Cosa hai messo sul banco mentre parlavi, in una riga.
    ///
    /// Mostrare e' un gesto e non una parola — il motore legge il tag, mai la
    /// prosa — e finora quel gesto non lasciava traccia da nessuna parte: uno
    /// che credeva di aver mostrato il braccialetto e non l'aveva fatto leggeva
    /// una trascrizione identica a quella di chi l'aveva mostrato davvero.
    ///
    /// I nomi escono dal catalogo, non dal testo del giocatore: qui dentro non
    /// puo' finire una parola che non sia d'autore.
    private string Didascalia(IReadOnlyList<string> shownItemIds)
    {
        if (shownItemIds.Count == 0)
        {
            return "";
        }
        var nomi = shownItemIds.Select(itemId =>
        {
            var oggetto = _items.Find(itemId);
            return oggetto is null || oggetto.Name.Length == 0 ? itemId : oggetto.Name;
        });
        return "mostri: " + string.Join(", ", nomi);
    }

    public async Task<TurnResult> TakeTurnAsync(string npcId, string rawText, CancellationToken cancellationToken = default)
    {
        // Le etichette le legge il motore, mai il modello: e' qui che si decide
        // cosa il giocatore possiede davvero e quali due righe ha raccolto.
        var utterance = PlayerInput.Parse(rawText, World, _items, _playerId);

        // Da qui in poi si lavora sulla copia. Niente di cio' che segue tocca il
        // mondo del giocatore finche' il turno non e' finito bene.
        var draft = World.Clone();
        var gradinoPrima = _declarations.PositionOf(npcId, World);

        // Mostrare precede il prompt, perche' e' cio' che si ha davanti a decidere
        // quale porzione della propria colonna un personaggio abbia in mano.
        foreach (var itemId in utterance.ShownItemIds)
        {
            draft.MarkShown(npcId, itemId);
        }

        // La frase non e' un oggetto: e' cinque parole scritte sulla prima pagina
        // del taccuino, e il modo di usarle e' dirle. Il motore le riconosce
        // mentre le scrivi e le segna come messe davanti a questa persona —
        // percio' tutto cio' che gia' dipendeva dall'averla mostrata continua a
        // valere senza cambiare una riga.
        var fraseDetta = Frase.Detta(utterance.Spoken);
        if (fraseDetta)
        {
            draft.MarkShown(npcId, "frase");
        }

        // Il rimando a Nino. Quando lo stallo con Matteo e' un fatto — Elena
        // risulta viva e lui ha detto di no — il paese lo sa prima di te: la
        // PRIMA persona con cui parli ti manda da Nino, che ha una cosa da
        // darti. Una volta sola, e mai da Nino o Matteo stessi: e' un passa-
        // parola, non un coro.
        var note = new List<string>();
        var registro = new Register(draft);
        if (npcId != "nino" && npcId != "matteo" && npcId != "wanda" && npcId != "elena"
            && !draft.Flags.ContainsKey("nino_indirizzato")
            && registro.IsEstablished("elena_viva")
            && registro.IsEstablished("matteo_non_dice_dove"))
        {
            note.Add("Nino Bergesio sta facendo dire in giro che cerca il ragazzo dei Lipari: "
                + "tiene una cosa da dargli, da anni. Diglielo prima che il discorso finisca — "
                + "che Nino lo cerca, e che e' meglio che ci passi.");
            draft.Flags["nino_indirizzato"] = true;
        }

        var turn = new TurnContext
        {
            Spoken = utterance.Spoken,
            ShownItemIds = utterance.ShownItemIds,
            ClockText = WorldClock.Format(draft.Minute),
            // Se cio' che e' finito sul banco ha fatto salire il personaggio di
            // gradino, questo e' un momento in cui la storia si muove: il
            // costruttore del prompt lo dice, e la risposta puo' allungarsi.
            Svolta = _declarations.PositionOf(npcId, draft) != gradinoPrima,
            FraseDetta = fraseDetta,
            NpcNotes = note,
        };

        var messages = _context.Build(npcId, draft, Log.Recent(npcId, HistoryWindow), turn).ToWire();

        var stopwatch = Stopwatch.StartNew();
        var reply = await _transport
            .ChatAsync(_model, messages, ToolCatalog.SchemasFor(_declarations.EverSayable(npcId)), cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();

        if (!reply.IsOk)
        {
            // Il mondo non si e' mosso di un minuto. E' il punto di tutta la copia.
            return TurnResult.Fail(reply.Code, reply.Message);
        }

        var declared = new List<string>();
        var refused = new List<string>();
        foreach (var call in reply.Value!.ToolCalls)
        {
            if (call.Name != "dichiaro")
            {
                continue;
            }
            var id = call.Arguments.TryGetProperty("id", out var value) ? value.GetString() ?? "" : "";
            var outcome = _declarations.Declare(draft, npcId, id);
            (outcome.IsOk ? declared : refused).Add(id);
        }

        // Un modello che chiama lo strumento e non scrive niente lascia il
        // giocatore davanti al silenzio: ha parlato, ha aspettato, e sullo
        // schermo non compare nessuno — mentre il taccuino, lui, si riempie.
        // Succede quando il turno finisce con la sola chiamata, ed e' proprio
        // nei momenti che contano, perche' e' li' che qualcosa viene dichiarato.
        // Quando capita, la battuta e' quella che il personaggio stava
        // segnalando: il testo c'e' gia', scritto da noi, e nella sua variante.
        var detto = reply.Value.Text;
        if (string.IsNullOrWhiteSpace(detto) && declared.Count > 0)
        {
            detto = string.Join(" ", declared.Select(id => _declarations.TextOf(id, npcId)));
        }

        // SICUREZZA: il registro viene rigiocato intatto nei prompt dei turni
        // successivi, quindi ci entra solo testo gia' definitivo. Le parole del
        // giocatore sono ovviamente non fidate; quelle del modello lo sono
        // altrettanto, perche' un turno passato che contenesse un blocco forgiato
        // tornerebbe nel prompt come se il mondo l'avesse scritto.
        Log.Append(npcId, ChatRole.User, PlayerInput.Sanitize(utterance.Spoken), Didascalia(utterance.ShownItemIds));
        Log.Append(npcId, ChatRole.Assistant, PlayerInput.Sanitize(detto));

        // La consegna: gli oggetti che i gradini raggiunti hanno da dare
        // passano di mano ADESSO, decisi dai dati e mai dal modello. Una volta
        // in tasca al giocatore non tornano indietro: e' una consegna, non un
        // prestito.
        var ricevuti = new List<string>();
        foreach (var itemId in _declarations.ConsegnateFinora(npcId, draft))
        {
            if (!draft.ItemOwners.TryGetValue(itemId, out var chi) || chi != _playerId)
            {
                draft.ItemOwners[itemId] = _playerId;
                ricevuti.Add(itemId);
            }
        }

        // Uno scambio costa un minuto. L'attesa della rete non costa niente: e'
        // latenza dell'infrastruttura, non una scelta del giocatore, e farla
        // pagare renderebbe il gioco piu' difficile quando la linea e' lenta.
        draft.Minute += WorldClock.DialogueTurnMinutes;

        World = draft;

        return new TurnResult
        {
            Received = ricevuti,
            IsOk = true,
            NpcId = npcId,
            Reply = detto,
            RefusedTags = utterance.InvalidTags,
            Declared = declared,
            RefusedDeclarations = refused,
            Minute = draft.Minute,
        };
    }
}
