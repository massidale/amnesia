using System.Text.Json;
using Amnesia.Core;

namespace Amnesia.Persistence;

/// Un salvataggio e' lo stato del mondo piu' un numero di versione. Il numero
/// esiste perche' un salvataggio vecchio deve essere rifiutato a voce alta
/// invece di caricarsi a meta'.
public sealed class SaveCodec
{
    public const int Version = 1;

    private sealed class Envelope
    {
        public int Version { get; set; }
        public string State { get; set; } = "";
    }

    public string Encode(WorldState state) =>
        JsonSerializer.Serialize(new Envelope { Version = Version, State = state.ToJson() });

    public Result<WorldState> Decode(string text)
    {
        Envelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<Envelope>(text);
        }
        catch (JsonException)
        {
            return Result<WorldState>.Fail("invalid_json", "il salvataggio non e' un oggetto valido");
        }

        if (envelope is null)
        {
            return Result<WorldState>.Fail("invalid_json", "il salvataggio non e' un oggetto valido");
        }

        if (envelope.Version != Version)
        {
            return Result<WorldState>.Fail("unsupported_save_version", envelope.Version.ToString());
        }

        return Result<WorldState>.Ok(WorldState.FromJson(envelope.State));
    }
}
