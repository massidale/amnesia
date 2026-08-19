using Amnesia.Core;
using Amnesia.Knowledge;
using NUnit.Framework;

namespace Amnesia.Tests.Core;

public class WorldStateTests
{
    [Test]
    public void UnMondoNuovoParteAlleNove()
    {
        Assert.That(new WorldState().Minute, Is.EqualTo(540));
    }

    [Test]
    public void LoStatoSopravviveAlGiroSulDisco()
    {
        var world = new WorldState { Minute = 612 };
        world.ActorOf("matteo").Position = new Cell(4, 7);
        world.MarkShown("matteo", "frase");
        world.Flags["incipit_letto"] = true;
        new KnowledgeService(world).RevealFact("matteo", "cava_crollata", "la cava e' crollata", "vissuto", 1.0);

        var reloaded = WorldState.FromJson(world.ToJson());

        Assert.That(reloaded.Minute, Is.EqualTo(612));
        Assert.That(reloaded.ActorOf("matteo").Position, Is.EqualTo(new Cell(4, 7)));
        Assert.That(reloaded.ShownToNpc("matteo"), Is.EqualTo(new[] { "frase" }));
        Assert.That(reloaded.Flags["incipit_letto"], Is.True);
        Assert.That(new KnowledgeService(reloaded).Knows("matteo", "cava_crollata"), Is.True);
    }

    [Test]
    public void UnaCopiaNonCondivideNienteConLOriginale()
    {
        var world = new WorldState();
        world.MarkShown("matteo", "frase");

        var copy = world.Clone();
        copy.MarkShown("matteo", "fotografia");
        copy.Minute = 900;

        Assert.That(world.ShownToNpc("matteo"), Is.EqualTo(new[] { "frase" }), "l'originale non si muove");
        Assert.That(world.Minute, Is.EqualTo(540));
    }

    [Test]
    public void MostrareDueVolteLaStessaCosaNonLaConteggiaDueVolte()
    {
        var world = new WorldState();
        world.MarkShown("matteo", "frase");
        world.MarkShown("matteo", "frase");

        Assert.That(world.ShownToNpc("matteo").Count, Is.EqualTo(1));
    }

    [Test]
    public void UnPersonaggioACuiNonEStatoMostratoNienteRendeUnaListaVuota()
    {
        Assert.That(new WorldState().ShownToNpc("nessuno"), Is.Empty);
    }
}
