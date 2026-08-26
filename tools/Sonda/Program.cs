using System.Text.Json;
using Amnesia;
using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Game;
using Amnesia.Llm;

// La sonda: il motore vero, senza rete. Il transport scrive il prompt su file
// e aspetta che qualcuno (un altro modello, un umano) depositi la risposta.
// Serve a collaudare i prompt con un attore qualsiasi al posto di OpenRouter.
//
// Protocollo, dentro la cartella passata come secondo argomento:
//   in/turn-N.json    {"npc":"anna","testo":"..."}  oppure {"cmd":"saluto","npc":"anna"}
//   out/prompt-N.json {"messaggi":[{"role","testo"}],"strumenti":[...]}   (scritto dalla sonda)
//   in/reply-N.json   {"testo":"...","dichiaro":["id",...]}               (scritto dall'attore)
//   out/result-N.json esito del turno + taccuino + posizioni
//   in/fine           (file qualsiasi: la sonda esce)

var contenuto = args.Length > 0 ? args[0] : "content";
var cartella = args.Length > 1 ? args[1] : "sonda-run";
Directory.CreateDirectory(Path.Combine(cartella, "in"));
Directory.CreateDirectory(Path.Combine(cartella, "out"));

string Percorso(params string[] parti) => Path.Combine(new[] { contenuto }.Concat(parti).ToArray());

var opzioni = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

// --- caricamento, identico a Bootstrap.Carica ---
var itemsJson = JsonDocument.Parse(File.ReadAllText(Percorso("items.json")));
var definizioni = new List<ItemDefinition>();
foreach (var riga in itemsJson.RootElement.EnumerateArray())
{
    definizioni.Add(new ItemDefinition(
        riga.GetProperty("id").GetString() ?? "",
        riga.TryGetProperty("visible", out var v) ? v.GetString() ?? "" : "",
        riga.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
        riga.TryGetProperty("description", out var d) ? d.GetString() ?? "" : ""));
}
var items = new ItemCatalog(definizioni);

var dichiarazioni = DeclarationTable.Load(Percorso("amnesia", "declarations.json"));
var posizioni = PositionTable.Load(Percorso("amnesia", "positions.json"));
var saluti = GreetingTable.Load(Percorso("amnesia", "saluti.json"));
var convinzioni = ConvinzioniTable.Load(Percorso("amnesia", "taccuino.json"));
var reazioni = ReactionTable.Load(Percorso("amnesia", "reazioni.json"));
if (!dichiarazioni.IsOk || !posizioni.IsOk || !saluti.IsOk || !convinzioni.IsOk || !reazioni.IsOk)
{
    Console.Error.WriteLine("contenuto illeggibile: " +
        string.Join(" | ", new[] { dichiarazioni.Message, posizioni.Message, saluti.Message, convinzioni.Message, reazioni.Message }.Where(m => !string.IsNullOrEmpty(m))));
    return 1;
}

var world = new WorldState();
foreach (var starting in new[] { "fotografia", "foglio_indirizzo", "chiave_b17" })
{
    world.ItemOwners[starting] = "player";
}

var schede = Amnesia.Dialogue.PromptLibrary.Load(Percorso("prompts"));
var regole = Amnesia.Dialogue.PromptLibrary.SharedPrefix(Percorso("prompts"));
var conoscenze = Amnesia.Dialogue.ConoscenzeBase.Load(Percorso("prompts"));

var tabelle = new DeclarationService(dichiarazioni.Value!, posizioni.Value!);
var contesto = new ContextBuilder(regole, schede, items, false, dichiarazioni.Value, posizioni.Value, reazioni.Value, conoscenze);
var trasporto = new FileTransport(cartella, opzioni);
var log = new ConversationLog();
var sessione = new ConversationSession(world, log, contesto, trasporto, tabelle, items, "player");
var accoglienza = new Greeter(saluti.Value!, posizioni.Value!);
var guardinghi = new[] { "anna", "laura", "matteo", "don_carlo", "nino", "don carlo" }.Where(posizioni.Value!.HasLadder).ToArray();

Console.WriteLine($"sonda pronta su {cartella}");

for (var n = 1; ; n++)
{
    var turnoPath = Path.Combine(cartella, "in", $"turn-{n}.json");
    while (!File.Exists(turnoPath))
    {
        if (File.Exists(Path.Combine(cartella, "in", "fine"))) { return 0; }
        Thread.Sleep(150);
    }
    Thread.Sleep(100); // il file potrebbe essere a meta' scrittura
    var turno = JsonDocument.Parse(File.ReadAllText(turnoPath)).RootElement;
    var npc = turno.GetProperty("npc").GetString() ?? "";
    object esito;
    if (turno.TryGetProperty("cmd", out var cmd0) && cmd0.GetString() == "prendi")
    {
        // Il ritiro fisico di un oggetto in un luogo: nel gioco e' una porta
        // della mappa, qui lo concede l'orchestratore quando la fiction lo
        // giustifica (es. il magazzino, una volta saputo dov'e').
        var item = turno.GetProperty("item").GetString() ?? "";
        sessione.World.ItemOwners[item] = "player";
        esito = Stato(true, npc, "", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), new[] { item }, "");
    }
    else if (turno.TryGetProperty("cmd", out var cmd) && cmd.GetString() == "saluto")
    {
        trasporto.Turno = n;
        var riga = accoglienza.Apri(sessione.World, log, npc);
        esito = Stato(ok: true, npc, riga, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), "");
    }
    else
    {
        trasporto.Turno = n;
        var testo = turno.GetProperty("testo").GetString() ?? "";
        var risultato = await sessione.TakeTurnAsync(npc, testo);
        esito = risultato.IsOk
            ? Stato(true, npc, risultato.Reply, risultato.Declared, risultato.RefusedDeclarations, risultato.RefusedTags, risultato.Received, "")
            : Stato(false, npc, "", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), risultato.Message);
    }
    File.WriteAllText(Path.Combine(cartella, "out", $"result-{n}.json"), JsonSerializer.Serialize(esito, opzioni));
}

object Stato(bool ok, string npc, string risposta, IReadOnlyList<string> dichiarate, IReadOnlyList<string> rifiutate,
    IReadOnlyList<string> tagRifiutati, IReadOnlyList<string> ricevuti, string errore)
{
    var taccuino = new Taccuino(sessione.World, convinzioni.Value!, items, "player");
    return new
    {
        ok,
        npc,
        risposta,
        dichiarate,
        rifiutate,
        tagRifiutati,
        ricevuti,
        errore,
        minuto = sessione.World.Minute,
        posizioni = guardinghi.ToDictionary(id => id, id => tabelle.PositionOf(id, sessione.World)),
        mostratiA = sessione.World.ShownTo.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value),
        taccuino = taccuino.Convinzioni().Select(r => new { r.Id, r.Testo, r.Cancellata }),
        tasche = taccuino.Tasche().Select(o => o.Id),
        registro = sessione.World.Declarations,
    };
}

/// Scrive il prompt su out/prompt-N.json e aspetta in/reply-N.json.
sealed class FileTransport : IChatTransport
{
    private readonly string _cartella;
    private readonly JsonSerializerOptions _opzioni;
    public int Turno { get; set; }

    public FileTransport(string cartella, JsonSerializerOptions opzioni)
    {
        _cartella = cartella;
        _opzioni = opzioni;
    }

    public async Task<Result<LlmReply>> ChatAsync(
        string model, IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        var prompt = new
        {
            model,
            messaggi = messages.Select(m => new
            {
                role = m.Role,
                testo = m.ContentBlocks is { Count: > 0 }
                    ? string.Join("", m.ContentBlocks.Select(b => b.Text))
                    : m.Content ?? "",
            }),
            strumenti = tools,
        };
        File.WriteAllText(Path.Combine(_cartella, "out", $"prompt-{Turno}.json"), JsonSerializer.Serialize(prompt, _opzioni));

        var replyPath = Path.Combine(_cartella, "in", $"reply-{Turno}.json");
        while (!File.Exists(replyPath))
        {
            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }
        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        var reply = JsonDocument.Parse(File.ReadAllText(replyPath)).RootElement;
        var testo = reply.TryGetProperty("testo", out var t) ? t.GetString() ?? "" : "";
        var chiamate = new List<ToolCall>();
        if (reply.TryGetProperty("dichiaro", out var dichiara))
        {
            var i = 0;
            foreach (var id in dichiara.EnumerateArray())
            {
                var raw = JsonSerializer.Serialize(new { id = id.GetString() });
                chiamate.Add(new ToolCall
                {
                    Id = $"call_{Turno}_{i++}",
                    Name = "dichiaro",
                    Arguments = JsonDocument.Parse(raw).RootElement.Clone(),
                    RawArguments = raw,
                });
            }
        }
        return Result<LlmReply>.Ok(new LlmReply { Text = testo, ToolCalls = chiamate, FinishReason = "stop" });
    }
}
