using Amnesia.Core;
using System.Linq;
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
        return string.Join("\n", messages.SelectMany(m => m.Parts).Select(x => x.Text));
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

    /// Chi non ha una scala ha davanti tutto cio' di cui e' fonte, subito.
    ///
    /// Prima il blocco non c'era affatto, e sembrava coerente — uno che non
    /// nasconde niente non ha una posizione da tenere. Ma il risultato era che
    /// Rosa e i paesani non avevano il testo delle righe che il motore gli
    /// permette di dire, e le improvvisavano: e un personaggio che improvvisa
    /// su una riga improvvisa anche sul resto, fino a inventarsi dove sta il
    /// magazzino.
    [Test]
    public void ChiNonHaUnaScalaHaSubitoTuttoCioDiCuiEFonte()
    {
        var prompt = Prompt("rosa", TestDeclarations.World());

        Assert.That(prompt, Does.Contain("<posizione>"));
        Assert.That(prompt, Does.Contain("l'affitto di un posto"), "una riga di cui Rosa e' fonte");
        Assert.That(prompt, Does.Not.Contain("nel castagneto"), "e nessuna di cui non lo e'");
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

    /// Il paesano deve *avere* le righe di cui e' fonte. Senza, le improvvisa —
    /// e il coro, che e' la prova migliore del gioco, si scioglie: la stessa
    /// frase impossibile da tre bocche diverse funziona solo se le tre bocche
    /// dicono le stesse parole.
    [Test]
    public void UnPaesanoHaLaVersioneDelPaeseDavanti()
    {
        var prompt = Prompt("teresa", new WorldState());

        Assert.That(prompt, Does.Contain("è stato un attimo"));
    }

    /// E soprattutto: quello di cui non e' fonte non ce l'ha. La vecchia del
    /// giardino non sa dove sia il magazzino, e nel suo prompt quella riga non
    /// esiste — se la dice se l'e' inventata, e le regole glielo vietano.
    [Test]
    public void UnPaesanoNonSaDoveSiaIlMagazzino()
    {
        var prompt = Prompt("teresa", new WorldState());

        Assert.That(prompt, Does.Not.Contain("magazzino"));
        Assert.That(prompt, Does.Not.Contain("diciassette"));
    }

    /// Anna lo sa, ma non prima: la riga entra nel suo prompt il turno in cui
    /// le si dice la frase, non un momento prima.
    [Test]
    public void AnnaHaIlMagazzinoSoloDopoLaFrase()
    {
        var world = new WorldState();

        Assert.That(Prompt("anna", world), Does.Not.Contain("diciassette"));

        world.MarkShown("anna", "frase");

        Assert.That(Prompt("anna", world), Does.Contain("diciassette"));
    }
}
