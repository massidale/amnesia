using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Dialogue;

public class TurnTelemetryTests
{
    private string _path = "";

    [SetUp]
    public void SetUp() => _path = Path.Combine(Path.GetTempPath(), $"amnesia_telemetry_{Guid.NewGuid():N}.jsonl");

    [TearDown]
    public void TearDown() => File.Delete(_path);

    [Test]
    public void OgniTurnoLasciaLaSuaRigaNellOrdineInCuiEStatoGiocato()
    {
        var telemetry = new TurnTelemetry(_path);
        telemetry.Record(new TurnRecord
        {
            NpcId = "giorgio",
            LatencyMs = 1200,
            PromptTokens = 900,
            CompletionTokens = 120,
            AuthoredNotes = new[] { "catena_incipit" },
        });
        telemetry.Record(new TurnRecord { NpcId = "giulia", LatencyMs = 800 });

        var entries = telemetry.Entries();

        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries[0].NpcId, Is.EqualTo("giorgio"));
        Assert.That(entries[0].PromptTokens, Is.EqualTo(900), "la spesa in token si rivede a sessione finita");
        Assert.That(entries[0].AuthoredNotes, Is.EqualTo(new[] { "catena_incipit" }), "il canale d'autore non e' muto anche per il registro");
        Assert.That(entries[1].NpcId, Is.EqualTo("giulia"));
    }

    [Test]
    public void UnFileCheNonEsisteAncoraNonEUnErrore()
    {
        Assert.That(new TurnTelemetry(_path).Entries(), Is.Empty);
    }

    [Test]
    public void UnaRigaMoncaNonRendeIllegibiliQuelleIntere()
    {
        // Una sessione uccisa a meta' di una scrittura lascia esattamente questo.
        var telemetry = new TurnTelemetry(_path);
        telemetry.Record(new TurnRecord { NpcId = "giorgio" });
        File.AppendAllText(_path, "{\"NpcId\": \"gi\n\n");
        telemetry.Record(new TurnRecord { NpcId = "giulia" });

        var entries = telemetry.Entries();

        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries[1].NpcId, Is.EqualTo("giulia"));
    }

    [Test]
    public void UnPercorsoNonScrivibileNonFaCadereIlGioco()
    {
        // La telemetria e' un audit, non una meccanica: se il disco dice di no, il
        // turno si gioca lo stesso.
        var telemetry = new TurnTelemetry(Path.Combine(_path, "sotto_un_file", "telemetria.jsonl"));

        Assert.DoesNotThrow(() => telemetry.Record(new TurnRecord { NpcId = "giorgio" }));
        Assert.That(telemetry.Entries(), Is.Empty);
    }
}
