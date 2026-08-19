using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amnesia.Dialogue;

/// Una riga di audit: un turno di dialogo concluso.
public sealed record TurnRecord
{
    public string NpcId { get; init; } = "";
    public int LatencyMs { get; init; }

    /// Quanto e' costato il turno. In GDScript era il dizionario `usage` che il
    /// provider rimandava indietro cosi' com'era; qui sono i due numeri su cui si
    /// rivede la spesa, e come il provider li chiami lo sa solo il suo adattatore.
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }

    /// Il canale d'autore e' muto per il giocatore per scelta; muto anche per il
    /// registro non deve esserlo, o dopo una partita nessuno puo' piu' dire quali
    /// catene si siano davvero accese.
    public IReadOnlyList<string> AuthoredNotes { get; init; } = Array.Empty<string>();
}

/// Traccia di audit in sola aggiunta, una riga JSON per turno concluso: a
/// sessione finita si rivedono la spesa in token e le derive di comportamento
/// (design §16).
public sealed class TurnTelemetry
{
    public string Path { get; }

    public TurnTelemetry(string filePath) => Path = filePath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Record(TurnRecord entry)
    {
        try
        {
            // Una riga sola, terminata a mano: un file JSONL si legge riga per riga
            // anche mentre lo si sta ancora scrivendo, e la fine di riga non puo'
            // dipendere dal sistema su cui gira la sessione.
            File.AppendAllText(Path, JsonSerializer.Serialize(entry, SerializerOptions) + "\n");
        }
        catch (IOException)
        {
            // La telemetria non deve mai far cadere il gioco: un percorso bloccato
            // o non scrivibile si salta.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public IReadOnlyList<TurnRecord> Entries()
    {
        string[] lines;
        try
        {
            if (!File.Exists(Path))
            {
                return Array.Empty<TurnRecord>();
            }
            lines = File.ReadAllLines(Path);
        }
        catch (IOException)
        {
            return Array.Empty<TurnRecord>();
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<TurnRecord>();
        }

        var entries = new List<TurnRecord>();
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0)
            {
                continue;
            }
            try
            {
                var parsed = JsonSerializer.Deserialize<TurnRecord>(line, SerializerOptions);
                if (parsed is not null)
                {
                    entries.Add(parsed);
                }
            }
            catch (JsonException)
            {
                // Una riga monca — la sessione uccisa a meta' di una scrittura —
                // non deve rendere illeggibili quelle intere che la precedono.
            }
        }
        return entries;
    }
}
