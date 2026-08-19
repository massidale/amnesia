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

    public async Task<TurnResult> TakeTurnAsync(string npcId, string rawText, CancellationToken cancellationToken = default)
    {
        // Le etichette le legge il motore, mai il modello: e' qui che si decide
        // cosa il giocatore possiede davvero e quali due righe ha raccolto.
        var utterance = PlayerInput.Parse(rawText, World, _items, _playerId);

        // Da qui in poi si lavora sulla copia. Niente di cio' che segue tocca il
        // mondo del giocatore finche' il turno non e' finito bene.
        var draft = World.Clone();

        // Mostrare precede il prompt, perche' e' cio' che si ha davanti a decidere
        // quale porzione della propria colonna un personaggio abbia in mano.
        foreach (var itemId in utterance.ShownItemIds)
        {
            draft.MarkShown(npcId, itemId);
        }

        // Un confronto conta per chi se l'e' visto davanti: convincere Anna non
        // convince Matteo, ed e' proprio quello il lavoro da fare in bottega.
        if (utterance.Confronto is { } pair)
        {
            draft.MarkConfrontoShown(npcId, pair.First, pair.Second);
        }

        var turn = new TurnContext
        {
            Spoken = utterance.Spoken,
            ShownItemIds = utterance.ShownItemIds,
            Confronto = utterance.Confronto,
            ClockText = WorldClock.Format(draft.Minute),
        };

        var messages = _context.Build(npcId, draft, Log.Recent(npcId, HistoryWindow), turn).ToWire();

        var stopwatch = Stopwatch.StartNew();
        var reply = await _transport.ChatAsync(_model, messages, ToolCatalog.Schemas(), cancellationToken)
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

        // SICUREZZA: il registro viene rigiocato intatto nei prompt dei turni
        // successivi, quindi ci entra solo testo gia' definitivo. Le parole del
        // giocatore sono ovviamente non fidate; quelle del modello lo sono
        // altrettanto, perche' un turno passato che contenesse un blocco forgiato
        // tornerebbe nel prompt come se il mondo l'avesse scritto.
        Log.Append(npcId, ChatRole.User, PlayerInput.Sanitize(utterance.Spoken));
        Log.Append(npcId, ChatRole.Assistant, PlayerInput.Sanitize(reply.Value.Text));

        // Uno scambio costa un minuto. L'attesa della rete non costa niente: e'
        // latenza dell'infrastruttura, non una scelta del giocatore, e farla
        // pagare renderebbe il gioco piu' difficile quando la linea e' lenta.
        draft.Minute += WorldClock.DialogueTurnMinutes;

        World = draft;

        return new TurnResult
        {
            IsOk = true,
            NpcId = npcId,
            Reply = reply.Value.Text,
            RefusedTags = utterance.InvalidTags,
            Declared = declared,
            RefusedDeclarations = refused,
            Minute = draft.Minute,
        };
    }
}
