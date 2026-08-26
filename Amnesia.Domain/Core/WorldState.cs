using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amnesia.Core;

/// Tutto lo stato di una partita, e nient'altro. Serializzabile per intero: un
/// salvataggio e' questo oggetto, e ogni cosa che il gioco deve ricordare vive
/// qui dentro e non nei campi di un servizio.
///
/// Rispetto alla versione GDScript ogni sottosistema ha il suo stato tipizzato
/// invece di una chiave dentro un sacco di dizionari: la flessibilita' di quel
/// sacco serviva a un linguaggio senza record, e costava un cast a ogni lettura.
public sealed class WorldState
{
    public const int DefaultStartMinute = 540; // 9:00

    public int Minute { get; set; } = DefaultStartMinute;

    public Dictionary<string, Actor> Actors { get; set; } = new();

    /// Chi possiede cosa.
    public Dictionary<string, string> ItemOwners { get; set; } = new();

    /// Cosa e' stato mostrato a chi. Guida le scale di posizione, e con esse
    /// quali righe entrano nel prompt di un personaggio.
    public Dictionary<string, List<string>> ShownTo { get; set; } = new();

    /// I confronti gia' messi davanti a ciascun personaggio, come coppie
    /// normalizzate. Sta accanto a ShownTo e non dentro, perche' accostare due

    /// Le catene autoriali a colpo singolo: acceso una volta, resta acceso.
    public Dictionary<string, bool> Flags { get; set; } = new();

    /// Il registro delle dichiarazioni: chi ha detto cosa, e quante volte. E'
    /// stato del mondo come tutto il resto — sopravvive a un salvataggio, e due
    /// registri sullo stesso mondo concordano perche' leggono di qui.
    /// In GDScript era una chiave dentro il sacco dei flag; qui i flag sono
    /// booleani, e questo e' cio' che e' sempre stato: una riga per dichiarazione.
    public Dictionary<string, RegisterEntry> Declarations { get; set; } = new();

    /// Dove qualcuno ha mandato un personaggio, a dispetto della sua routine.
    /// In GDScript era una chiave dentro il sacco dei flag, che li' erano
    /// dizionari di qualunque cosa; qui i flag sono booleani e questo e' un
    /// dizionario di destinazioni, che e' cio' che e' sempre stato.
    public Dictionary<string, string> MovementIntents { get; set; } = new();

    /// Il resto frazionario di un passo, per personaggio. Sta qui e non nei campi
    /// del sistema di movimento perche' deve sopravvivere a un salvataggio, e
    /// perche' due partite guidate dallo stesso sistema non devono scambiarselo.
    public Dictionary<string, double> MovementCarry { get; set; } = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    public static WorldState FromJson(string json) =>
        JsonSerializer.Deserialize<WorldState>(json, SerializerOptions)
        ?? throw new JsonException("stato del mondo nullo");

    /// Una copia profonda passando dal giro di serializzazione, cosi' non esiste
    /// un secondo modo di duplicare lo stato che possa divergere dal primo.
    public WorldState Clone() => FromJson(ToJson());

    public Actor ActorOf(string actorId)
    {
        if (!Actors.TryGetValue(actorId, out var actor))
        {
            actor = new Actor();
            Actors[actorId] = actor;
        }
        return actor;
    }

    public IReadOnlyList<string> ShownToNpc(string npcId) =>
        ShownTo.TryGetValue(npcId, out var shown) ? shown : Array.Empty<string>();
    public void MarkShown(string npcId, string itemId)
    {
        if (!ShownTo.TryGetValue(npcId, out var shown))
        {
            shown = new List<string>();
            ShownTo[npcId] = shown;
        }
        if (!shown.Contains(itemId))
        {
            shown.Add(itemId);
        }
    }
}
