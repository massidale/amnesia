using Amnesia.Core;
using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Cosa succede a chi salta la fila.
///
/// La chiave del magazzino Giorgio ce l'ha dal primo minuto: niente gli
/// impedisce di trovare il deposito il primo giorno, scendere in cantina e
/// portare il braccialetto ad Anna senza aver mai detto una parola della
/// frase. Un gioco a serrature si rompe proprio li' — o perde un pezzo per
/// strada, o consegna tutto insieme.
///
/// Qui si dimostra che non succede ne' l'uno ne' l'altro.
public class FuoriOrdineTests
{
    private static ContextBuilder Costruttore() => new(
        "regole",
        new System.Collections.Generic.Dictionary<string, string> { ["anna"] = "scheda" },
        new ItemCatalog(), useCacheControl: false,
        TestDeclarations.Table(), TestDeclarations.Positions());

    private static string Prompt(WorldState world)
    {
        var messaggi = Costruttore().Build("anna", world, System.Array.Empty<LoggedMessage>(),
            new TurnContext { Spoken = "guarda qui", ClockText = "9:00" });
        return messaggi[messaggi.Count - 1].Parts.Single().Text;
    }

    /// Il braccialetto da solo non apre niente: senza la frase, Anna resta la
    /// donna che ti racconta la disgrazia. Non e' un rifiuto — e' che un
    /// braccialetto, a chi non ha ancora capito con chi sta parlando, e' un
    /// braccialetto.
    [Test]
    public void IlBraccialettoPrimaDellaFraseNonScavalcaIlGradino()
    {
        var world = new WorldState();
        world.MarkShown("anna", "braccialetto");

        var prompt = Prompt(world);

        Assert.That(prompt, Does.Contain("è stato un attimo"), "la versione del paese, e basta");
        Assert.That(prompt, Does.Not.Contain("Circolo della Soglia"));
        Assert.That(prompt, Does.Not.Contain("Serviva il sangue"), "il rito non arriva scavalcando");
    }

    /// E non si perde niente: quello che le hai messo davanti resta messo
    /// davanti. Quando poi la frase arriva, sale di due gradini nello stesso
    /// turno e ti dice tutto insieme — che e' anche la scena giusta, perche' a
    /// quel punto ha in mano tutte e due le ragioni per parlare.
    [Test]
    public void QuandoPoiArrivaLaFraseSaleDiDueGradiniInsieme()
    {
        var world = new WorldState();
        world.MarkShown("anna", "braccialetto");
        world.MarkShown("anna", "frase");

        var prompt = Prompt(world);

        Assert.That(prompt, Does.Contain("Circolo della Soglia"), "il primo gradino");
        Assert.That(prompt, Does.Contain("Serviva il sangue"), "e il secondo, senza rimostrare niente");
        Assert.That(prompt, Does.Contain("era rimasto solo Matteo"), "compresa la riga che regge il terzo atto");
    }

    /// L'ordine inverso e' la partita normale, e da' lo stesso risultato: la
    /// scala non ricorda in che ordine le cose sono arrivate, solo che sono
    /// arrivate.
    [Test]
    public void LOrdineInversoDaLoStessoRisultato()
    {
        var primo = new WorldState();
        primo.MarkShown("anna", "braccialetto");
        primo.MarkShown("anna", "frase");

        var secondo = new WorldState();
        secondo.MarkShown("anna", "frase");
        secondo.MarkShown("anna", "braccialetto");

        Assert.That(Prompt(primo), Is.EqualTo(Prompt(secondo)));
    }
}
