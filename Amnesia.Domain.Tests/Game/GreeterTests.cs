using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Game;
using NUnit.Framework;

namespace Amnesia.Tests.Game;

public class GreeterTests
{
    private static GreetingTable Saluti() => GreetingTable.FromJson("""
    {"greetings": {
      "matteo": {"M0": "Giorgio! Guarda chi si vede in piedi.", "M1": "Ancora tu."},
      "rosa": {"": "Sei sceso. Hai mangiato qualcosa?"}
    }}
    """).Value;

    private static PositionTable Scale() => PositionTable.FromJson("""
    {"positions": {"matteo": [
      {"id": "M0", "grants": ["scampagnate"]},
      {"id": "M1", "grants": ["non_erano_gite"], "requires_shown": ["frase"]}
    ]}}
    """).Value;

    [Test]
    public void ChiNonHaScalaHaUnSalutoSolo()
    {
        var world = new WorldState();
        var log = new ConversationLog();

        var riga = new Greeter(Saluti(), Scale()).Apri(world, log, "rosa");

        Assert.That(riga, Is.EqualTo("Sei sceso. Hai mangiato qualcosa?"));
        Assert.That(log.Recent("rosa", 4).Single().Role, Is.EqualTo(ChatRole.Assistant),
            "il saluto entra nel registro, o il modello si contraddice al turno dopo");
    }

    [Test]
    public void NonSiRisalutaDueVolteDalloStessoGradino()
    {
        var world = new WorldState();
        var log = new ConversationLog();
        var accoglienza = new Greeter(Saluti(), Scale());

        accoglienza.Apri(world, log, "matteo");
        var seconda = accoglienza.Apri(world, log, "matteo");

        Assert.That(seconda, Is.Empty);
        Assert.That(log.Recent("matteo", 10).Count, Is.EqualTo(1));
    }

    /// La cosa che rende il saluto una meccanica invece che un ornamento: quando
    /// sei salito di un gradino, la prima cosa che senti e' che fra voi e'
    /// cambiato qualcosa — e nessuno te l'ha spiegato.
    [Test]
    public void SalireDiGradinoFaCambiareIlSaluto()
    {
        var world = new WorldState();
        var log = new ConversationLog();
        var accoglienza = new Greeter(Saluti(), Scale());
        accoglienza.Apri(world, log, "matteo");

        world.MarkShown("matteo", "frase");
        var dopo = accoglienza.Apri(world, log, "matteo");

        Assert.That(dopo, Is.EqualTo("Ancora tu."));
    }

    [Test]
    public void ChiNonHaUnaRigaTaceInveceDiDireQuellaDiUnAltro()
    {
        var riga = new Greeter(Saluti(), Scale()).Apri(new WorldState(), new ConversationLog(), "nino");

        Assert.That(riga, Is.Empty);
    }
}
