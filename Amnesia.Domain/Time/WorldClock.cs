namespace Amnesia.Time;

/// Un solo orologio per tutta la partita. L'attesa della rete non costa niente:
/// e' latenza dell'infrastruttura, non una scelta del giocatore, e farla pagare
/// renderebbe il gioco piu' difficile quando la connessione e' lenta.
public sealed class WorldClock
{
    public const int DialogueTurnMinutes = 1;

    /// L'ora come la leggerebbe un uomo con un orologio addosso.
    public static string Format(int minute) => $"{minute / 60:00}:{minute % 60:00}";

    /// La data e' fissa: la partita si svolge in una manciata d'ore di una sola
    /// mattina, e un turno costa un minuto — il giorno non gira. E' autorata qui
    /// perche' sia UNA sola per tutti: senza, alla domanda «che giorno e'?»
    /// ognuno ne inventava una diversa.
    private const string DataDelGiorno = "Martedì 13 ottobre 1987";

    /// Quando siamo, per il blocco condiviso del prompt: data autorata + ora dal
    /// clock. La stessa per ogni personaggio, perche' e' stato del mondo, non una
    /// cosa che ciascuno si ricorda a modo suo.
    public static string Quando(int minute)
    {
        var ora = minute / 60;
        var parte = ora < 12 ? "del mattino"
            : ora < 17 ? "del pomeriggio"
            : ora < 21 ? "di sera"
            : "di notte";
        return $"{DataDelGiorno}, le {Format(minute)} {parte}";
    }
}
