namespace Amnesia.Dialogue;

public enum ChatRole
{
    System,
    User,
    Assistant,
}

/// Un pezzo di messaggio. Il breakpoint di cache sta su una parte e non sul
/// messaggio perche' e' un punto nel testo — dove il fornitore puo' tagliare il
/// prefisso da riusare — e non una proprieta' del turno.
public sealed record PromptPart(string Text, bool CacheBreakpoint = false);

/// Un messaggio pronto per il fornitore. Una parte sola e nessun breakpoint e' la
/// forma piatta che ricevono i modelli senza cache; piu' parti sono la forma a
/// blocchi, l'unica che puo' portare un breakpoint.
public sealed record PromptMessage(ChatRole Role, IReadOnlyList<PromptPart> Parts)
{
    public static PromptMessage Plain(ChatRole role, string text) =>
        new(role, new[] { new PromptPart(text) });
}
