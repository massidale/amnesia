namespace Amnesia.Knowledge;

/// Una cosa che un personaggio ritiene vera, con da dove gli e' arrivata e quanto
/// ci crede. La fonte conta quanto la proposizione: un ricordo di Giorgio e una
/// cosa vista con i propri occhi non pesano uguale, e il prompt lo dice.
public sealed class Belief
{
    public string Proposition { get; set; } = "";
    public string SourceId { get; set; } = "";
    public double Confidence { get; set; }

    /// L'ordine in cui questa convinzione e' entrata nella testa del personaggio.
    /// Non e' cosmetico: le righe del prompt escono in quest'ordine, e i byte del
    /// prefisso devono essere identici fra un turno e l'altro o la cache manca.
    /// In GDScript l'ordine era quello d'inserimento del dizionario, garantito
    /// dal linguaggio; in .NET un Dictionary non lo garantisce, quindi va scritto.
    public int Sequence { get; set; }

    public static Belief Create(string proposition, string sourceId, double confidence, int sequence) => new()
    {
        Proposition = proposition,
        SourceId = sourceId,
        Confidence = Math.Clamp(confidence, 0.0, 1.0),
        Sequence = sequence,
    };
}
