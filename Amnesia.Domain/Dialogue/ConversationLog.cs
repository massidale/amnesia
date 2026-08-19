using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amnesia.Dialogue;

/// Una battuta gia' detta, come la rilegge il turno dopo. Non e' il messaggio che
/// va sul filo (quello e' `Amnesia.Llm.ChatMessage`, e sa di strumenti e di
/// chiamate): qui c'e' solo cio' che una conversazione ricorda di se' stessa.
public sealed record LoggedMessage(ChatRole Role, string Content);

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

    public void Append(string npcId, ChatRole role, string content)
    {
        if (!Histories.TryGetValue(npcId, out var history))
        {
            history = new List<LoggedMessage>();
            Histories[npcId] = history;
        }
        history.Add(new LoggedMessage(role, content));
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
