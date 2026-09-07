using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amnesia.Dialogue;

/// Una battuta gia' detta, come la rilegge il turno dopo. Non e' il messaggio che
/// va sul filo (quello e' `Amnesia.Llm.ChatMessage`, e sa di strumenti e di
/// chiamate): qui c'e' solo cio' che una conversazione ricorda di se' stessa.
///
/// `Didascalia` e' l'unica riga di tutto il gioco scritta dal motore: cosa hai
/// mostrato o ricevuto durante lo scambio. Sta qui e non nel pannello perche' la
/// trascrizione si ricompone da capo a ogni turno, e una riga tenuta a parte
/// sparirebbe appena riavvolgi. **Non entra nel prompt**: `ContextBuilder`
/// rigioca `Role` e `Content` e nient'altro, quindi il prefisso in cache resta
/// identico al byte e nessun modello impara a scrivere didascalie leggendo le
/// proprie — che e' esattamente cio' che le regole gli vietano di fare.
public sealed record LoggedMessage(ChatRole Role, string Content, string Didascalia = "");

/// La storia di chiacchiere per personaggio, dalla piu' vecchia alla piu' recente.
///
/// SICUREZZA: questo registro conserva alla lettera cio' che gli viene dato e non
/// neutralizza niente. ContextBuilder rigioca la storia intatta — solo il parlato
/// del turno corrente passa da PlayerInput.Sanitize — quindi chi chiama DEVE
/// aggiungere testo gia' definitivo, con le parole del giocatore gia'
/// neutralizzate, o un turno rigiocato puo' contrabbandare un blocco forgiato
/// dentro un prompt successivo.
public sealed class ConversationLog
{
    public Dictionary<string, List<LoggedMessage>> Histories { get; set; } = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        // I ruoli restano parole anche sul disco: un salvataggio lo si apre quando
        // qualcosa e' andato storto, e a quel punto un numero non dice niente.
        Converters = { new JsonStringEnumConverter() },
    };

    public void Append(string npcId, ChatRole role, string content, string didascalia = "")
    {
        if (!Histories.TryGetValue(npcId, out var history))
        {
            history = new List<LoggedMessage>();
            Histories[npcId] = history;
        }
        history.Add(new LoggedMessage(role, Compatta(content), didascalia.Trim()));
    }

    /// Una riga vuota non e' una cosa detta.
    ///
    /// Si toglie qui e non a schermo perche' il registro e' anche cio' che il
    /// modello si rilegge il turno dopo: lasciarcele dentro vuol dire mostrargli
    /// il proprio testo spaziato e insegnargli a continuare cosi'. Chi parla
    /// sull'uscio di casa non va a capo due volte per dare enfasi.
    internal static string Compatta(string content)
    {
        var pulito = SenzaDidascalie(content.Replace("\r\n", "\n").Replace('\r', '\n')).Trim();
        while (pulito.Contains("\n\n"))
        {
            pulito = pulito.Replace("\n\n", "\n");
        }
        return pulito;
    }

    /// Le didascalie non sono parlato.
    ///
    /// Le regole vietano di raccontare i gesti, e i modelli lo fanno lo stesso:
    /// «(la pialla si ferma di colpo)» prima della battuta. E' la stessa
    /// separazione fra le parole e i fatti su cui regge tutto il gioco — un
    /// personaggio puo' dire quello che vuole, ma non decide cosa succede — e
    /// una regola che il motore non fa rispettare, prima o poi, non e' una
    /// regola.
    ///
    /// Si toglie solo la riga *intera* fra parentesi o fra asterischi: una
    /// parentesi in mezzo a una frase e' parlato («era il '66, o il '67, non
    /// ricordo»), e toglierla mutilerebbe la battuta.
    private static string SenzaDidascalie(string content)
    {
        var righe = content.Split('\n');
        var tenute = new List<string>();
        foreach (var riga in righe)
        {
            var nuda = riga.Trim();
            var didascalia =
                (nuda.StartsWith("(") && nuda.EndsWith(")") && nuda.Length > 2)
                || (nuda.StartsWith("*") && nuda.EndsWith("*") && nuda.Length > 2)
                || (nuda.StartsWith("_") && nuda.EndsWith("_") && nuda.Length > 2);
            if (!didascalia)
            {
                tenute.Add(riga);
            }
        }
        // Se il modello ha scritto *soltanto* una didascalia, meglio la
        // didascalia spogliata che il silenzio: un personaggio muto e' un turno
        // perso, e il giocatore l'ha pagato.
        if (tenute.Count == 0 || tenute.All(riga => riga.Trim().Length == 0))
        {
            return content.Replace("(", "").Replace(")", "").Replace("*", "").Replace("_", "");
        }
        return string.Join("\n", tenute);
    }

    public IReadOnlyList<LoggedMessage> Recent(string npcId, int window)
    {
        if (!Histories.TryGetValue(npcId, out var full))
        {
            return Array.Empty<LoggedMessage>();
        }
        return full.Skip(Math.Max(full.Count - window, 0)).ToList();
    }

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    public static ConversationLog FromJson(string json) =>
        JsonSerializer.Deserialize<ConversationLog>(json, SerializerOptions)
        ?? throw new JsonException("registro di conversazione nullo");
}
