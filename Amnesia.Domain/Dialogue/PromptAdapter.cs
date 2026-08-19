using Amnesia.Llm;

namespace Amnesia.Dialogue;

/// Il ponte fra il prompt che il gioco compone e la forma che va sul filo.
///
/// Esiste per una ragione sola: un messaggio con piu' parti deve arrivare al
/// fornitore COME piu' parti. Appiattirlo in una stringa non rompe niente di
/// visibile — il modello legge lo stesso testo — ma butta via il breakpoint, e
/// da quel momento ogni richiesta ricalcola il prefisso invece di riusarlo.
/// E' il tipo di guasto che non si manifesta mai come errore: solo come conto.
public static class PromptAdapter
{
    public static ChatMessage ToWire(this PromptMessage message)
    {
        var role = message.Role switch
        {
            ChatRole.System => "system",
            ChatRole.User => "user",
            ChatRole.Assistant => "assistant",
            _ => throw new ArgumentOutOfRangeException(nameof(message)),
        };

        // Una parte sola e senza breakpoint resta piatta: e' la forma che
        // accettano anche i modelli che non fanno cache, e non ha senso
        // complicarla per niente.
        if (message.Parts.Count == 1 && !message.Parts[0].CacheBreakpoint)
        {
            return new ChatMessage { Role = role, Content = message.Parts[0].Text };
        }

        var blocks = message.Parts
            .Select(part => ContentBlock.Text_(part.Text, part.CacheBreakpoint))
            .ToList();
        return new ChatMessage { Role = role, ContentBlocks = blocks };
    }

    public static IReadOnlyList<ChatMessage> ToWire(this IEnumerable<PromptMessage> messages) =>
        messages.Select(ToWire).ToList();
}
