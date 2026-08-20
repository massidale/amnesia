using System.Linq;
using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Llm;

/// Il vocabolario chiuso ritagliato su chi parla.
///
/// Era uno solo per tutti: quarantaquattro identificativi parlanti, dati a ogni
/// personaggio a ogni turno. Teresa, che di quella storia non sa niente, si
/// ritrovava davanti `elena_viva` e `matteo_confessa` — e non gliene parlava il
/// blocco delle posizioni, che e' quello che tutti controllano: gliene parlava
/// l'elenco degli strumenti, che non guardava nessuno.
public class VocabolarioPerPersonaTests
{
    private static Amnesia.DeclarationService Servizio() =>
        new(Amnesia.Tests.Declarations.TestDeclarations.Table(),
            Amnesia.Tests.Declarations.TestDeclarations.Positions());

    private static string[] Vocabolario(string npcId) =>
        ToolCatalog.SchemasFor(Servizio().EverSayable(npcId))
            .Single(s => s.Function.Name == "dichiaro")
            .Function.Parameters.Properties["id"].EnumValues!.ToArray();

    [Test]
    public void UnaPaesanaNonVedeLaFineDellaStoria()
    {
        var suo = Vocabolario("teresa");

        Assert.That(suo, Has.No.Member("elena_viva"));
        Assert.That(suo, Has.No.Member("matteo_confessa"));
        Assert.That(suo, Has.No.Member("il_rito"));
        Assert.That(suo, Has.No.Member("sacrificio_per_vittorio"));
        Assert.That(suo, Contains.Item("versione_paese"), "ma quello che puo' dire c'e' tutto");
    }

    [Test]
    public void MatteoHaTuttiISuoiGradiniDalPrimoTurno()
    {
        var suo = Vocabolario("matteo");

        Assert.That(suo, Contains.Item("scampagnate"), "il gradino di partenza");
        Assert.That(suo, Contains.Item("matteo_confessa"), "e l'ultimo");
        Assert.That(suo, Has.No.Member("laura_assoggettata"), "ma non le righe di un altro");
    }

    /// L'elenco non deve cambiare mentre si gioca: se cambiasse a ogni gradino,
    /// ogni salita ricomprerebbe il prefisso del prompt che il fornitore tiene
    /// in cache — cioe' i sei secondi che abbiamo appena finito di togliere.
    [Test]
    public void LElencoNonCambiaMaiDuranteLaPartita()
    {
        var servizio = Servizio();

        Assert.That(servizio.EverSayable("matteo"), Is.EqualTo(servizio.EverSayable("matteo")));
        Assert.That(ToolCatalog.SchemasFor(servizio.EverSayable("anna")).Count,
            Is.EqualTo(ToolCatalog.SchemasFor(servizio.EverSayable("anna")).Count));
    }

    /// Un personaggio che non puo' dichiarare niente non ha lo strumento: una
    /// domanda senza risposte possibili e' solo un invito a sbagliare.
    [Test]
    public void ChiNonHaNienteDaDireNonHaLoStrumento()
    {
        var strumenti = ToolCatalog.SchemasFor(Servizio().EverSayable("uno_che_non_esiste"));

        Assert.That(strumenti.Any(s => s.Function.Name == "dichiaro"), Is.False);
        Assert.That(strumenti.Any(s => s.Function.Name == "end_conversation"), Is.True, "gli altri restano");
    }
}
