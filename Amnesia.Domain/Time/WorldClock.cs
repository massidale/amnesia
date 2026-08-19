namespace Amnesia.Time;

/// Un solo orologio per tutta la partita. L'attesa della rete non costa niente:
/// e' latenza dell'infrastruttura, non una scelta del giocatore, e farla pagare
/// renderebbe il gioco piu' difficile quando la connessione e' lenta.
public sealed class WorldClock
{
    public const int DialogueTurnMinutes = 1;
    public const int TravelMinutes = 3;
    public const int InspectionMinutes = 5;

    public int Minute { get; private set; }

    public WorldClock(int startMinute) => Minute = startMinute;

    public int Advance(int minutes)
    {
        if (minutes > 0)
        {
            Minute += minutes;
        }
        return Minute;
    }

    /// L'ora come la leggerebbe un uomo con un orologio addosso.
    public static string Format(int minute) => $"{minute / 60:00}:{minute % 60:00}";
}
