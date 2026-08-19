using Amnesia.Core;
using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Il test che vale l'intera fetta: qui si dimostra che una rivelazione
/// anticipata e' IMPOSSIBILE invece che scoraggiata. La riga che Matteo non ha
/// ancora non e' proibita nel suo prompt: non c'e'.
public class PositionPromptTests
{
    private static ContextBuilder Builder(string npcId) => new(
        "regole",
        new Dictionary<string, string> { [npcId] = $"scheda di {npcId}" },
        new ItemCatalog(),
        useCacheControl: false,
        TestDeclarations.Table(),
        TestDeclarations.Positions());

    private static string Prompt(string npcId, WorldState world)
    {
        var messages = Builder(npcId).Build(npcId, world, Array.Empty<LoggedMessage>(),
            new TurnContext { Spoken = "buongiorno", ClockText = "9:00" });
        return messages[messages.Count - 1].Parts.Single().Text;
    }

    [Test]
    public void SulPrimoGradinoLaCoperturaCEELaRigaCheLaSmentisceNo()
    {
        var cold = Prompt("matteo", TestDeclarations.World());

        Assert.That(cold, Does.Contain("<posizione>"), "il blocco esiste");
        Assert.That(cold, Does.Contain("scampagnate"), "la copertura e' nel prompt, come sua convinzione");
        Assert.That(cold, Does.Not.Contain("Non erano gite"),
            "e la riga che la smentisce NON c'e': non puo' sfuggirgli quello che non ha");
    }

    [Test]
    public void SulGradinoDopoLaRigaCompareEQuellaDiPrimaResta()
    {
        var told = Prompt("matteo", TestDeclarations.World("frase"));

        Assert.That(told, Does.Contain("Non erano gite"));
        Assert.That(told, Does.Contain("scampagnate"));
    }

    [Test]
    public void IlBloccoNonEUnaScaletta()
    {
        // Un modello a cui si dice "sei a M1" recita una progressione, e uno a cui
        // si dice "sai X ma non dirlo" prima o poi lo dice. Ne' il nome del gradino
        // ne' un'istruzione entrano mai qui: e' convinzione in prima persona.
        var told = Prompt("matteo", TestDeclarations.World("frase"));
        var cold = Prompt("matteo", TestDeclarations.World());

        Assert.That(told, Does.Not.Contain("M1"), "il nome della posizione non entra mai nel prompt");
        Assert.That(cold, Does.Not.Contain("M0"));
        Assert.That(told, Does.Not.Contain("non dire"), "nessuna istruzione a nascondere, mai");
    }

    [Test]
    public void ChiNonHaUnaScalaNonHaIlBlocco()
    {
        // Un personaggio senza scala non perde niente: il blocco semplicemente non
        // c'e'.
        Assert.That(Prompt("rosa", TestDeclarations.World()), Does.Not.Contain("<posizione>"));
    }

    [Test]
    public void SenzaLeTabelleIlPromptEQuelloDiPrima()
    {
        // Le tabelle sono facoltative: un chiamante che non le ha costruisce lo
        // stesso prompt di prima, e i test che lo difendono restano invariati.
        var plain = new ContextBuilder(
            "regole",
            new Dictionary<string, string> { ["matteo"] = "scheda di matteo" },
            new ItemCatalog(),
            useCacheControl: false);

        var messages = plain.Build("matteo", TestDeclarations.World("frase"), Array.Empty<LoggedMessage>(),
            new TurnContext { Spoken = "buongiorno", ClockText = "9:00" });

        Assert.That(messages[messages.Count - 1].Parts.Single().Text, Does.Not.Contain("<posizione>"));
    }
}
