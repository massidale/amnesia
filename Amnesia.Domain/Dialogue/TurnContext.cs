namespace Amnesia.Dialogue;

/// Tutto cio' che un turno porta al costruttore del prompt. In GDScript era un
/// dizionario di chiavi in stringa: una chiave scritta male diventava in silenzio
/// una stringa vuota, e il primo posto in cui te ne accorgevi era il prompt.
public sealed record TurnContext
{
    public string Spoken { get; init; } = "";

    /// Solo gli oggetti che PlayerInput ha davvero validato: qui dentro non arriva
    /// niente che il giocatore non possieda.
    public IReadOnlyList<string> ShownItemIds { get; init; } = Array.Empty<string>();

    public string ClockText { get; init; } = "";

    /// Cio' che e' arrivato a questo personaggio mentre il giocatore era altrove:
    /// il canale degli eventi ce l'ha lasciato, e il chiamante lo consuma una volta
    /// che questo prompt e' partito.
    public IReadOnlyList<string> NpcNotes { get; init; } = Array.Empty<string>();
}
