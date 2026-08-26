using Amnesia;
using Amnesia.Core;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Cosa succede a chi salta la fila. Il braccialetto Giorgio puo' portarlo ad
/// Anna senza aver mai detto la frase: la scala non si scavalca, ma non perde
/// niente per strada. Si verifica sulla posizione — dove il personaggio arriva —
/// non sul testo del prompt.
public class FuoriOrdineTests
{
    private static PositionTable Reali() =>
        PositionTable.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "Amnesia", "fixtures", "positions.json")).Value!;

    [Test]
    public void IlBraccialettoPrimaDellaFraseNonScavalcaIlGradino()
    {
        // A1 di Anna vuole la frase, A2 una prova della cava. Il braccialetto da
        // solo non basta: senza la frase, Anna resta a A0 — la donna che ti
        // racconta la disgrazia e basta.
        var world = new WorldState();
        world.MarkShown("anna", "braccialetto");
        Assert.That(Reali().PositionOf("anna", world), Is.EqualTo("A0"));
    }

    [Test]
    public void QuandoPoiArrivaLaFraseSaleDiDueGradiniInsieme()
    {
        // La frase soddisfa A1 e il braccialetto A2: due gradini nello stesso
        // turno, e Anna arriva al rito senza rimostrare niente.
        var world = new WorldState();
        world.MarkShown("anna", "braccialetto");
        world.MarkShown("anna", "frase");
        Assert.That(Reali().PositionOf("anna", world), Is.EqualTo("A2"));
    }

    [Test]
    public void LOrdineInversoDaLoStessoRisultato()
    {
        // La scala non ricorda in che ordine le cose sono arrivate, solo che sono
        // arrivate.
        var primo = new WorldState();
        primo.MarkShown("anna", "braccialetto");
        primo.MarkShown("anna", "frase");

        var secondo = new WorldState();
        secondo.MarkShown("anna", "frase");
        secondo.MarkShown("anna", "braccialetto");

        Assert.That(Reali().PositionOf("anna", primo),
            Is.EqualTo(Reali().PositionOf("anna", secondo)));
    }
}
