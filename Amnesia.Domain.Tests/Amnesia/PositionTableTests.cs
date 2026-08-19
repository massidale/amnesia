using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

public class PositionTableTests
{
    [Test]
    public void SenzaNienteInManoMatteoStaSulPrimoGradino()
    {
        var table = TestDeclarations.Positions();
        var cold = TestDeclarations.World();

        Assert.That(table.PositionOf("matteo", cold), Is.EqualTo("M0"));

        var granted = table.Granted("matteo", cold);
        Assert.That(granted, Does.Contain("scampagnate"), "a M0 ha la scampagnata");
        Assert.That(granted, Does.Contain("circolo_esisteva"), "e ammette che il gruppo esisteva");
        Assert.That(granted, Does.Not.Contain("non_erano_gite"), "ma NON ha la riga che smentisce la scampagnata");
    }

    [Test]
    public void CioCheGliEStatoMessoDavantiLoPortaSulGradinoDopo()
    {
        var table = TestDeclarations.Positions();
        var told = TestDeclarations.World("frase");

        Assert.That(table.PositionOf("matteo", told), Is.EqualTo("M1"), "chi gli ha detto la frase lo porta a M1");

        var granted = table.Granted("matteo", told);
        Assert.That(granted, Does.Contain("non_erano_gite"), "a M1 la riga c'e'");
        Assert.That(granted, Does.Contain("scampagnate"), "e i gradini si sommano: quello che aveva prima non gli viene tolto");
    }

    [Test]
    public void UnGradinoSaltatoTieneGiuTuttiQuelliSopra()
    {
        // Cio' che gli e' stato messo davanti non e' quello che serviva: la scala
        // non si percorre a punti, si sale in ordine.
        var table = TestDeclarations.Positions();
        var other = TestDeclarations.World("fotografia");

        Assert.That(table.PositionOf("matteo", other), Is.EqualTo("M0"));
        Assert.That(table.Granted("matteo", other), Does.Not.Contain("non_erano_gite"));
    }

    [Test]
    public void UnPersonaggioSenzaScalaNonEUnErrore()
    {
        // Non e' guardingo: non ha una posizione, e non gli si concede niente PER
        // POSIZIONE — quello che sa gli arriva dalla sua colonna epistemica.
        var table = TestDeclarations.Positions();

        Assert.That(table.PositionOf("rosa", TestDeclarations.World()), Is.Empty);
        Assert.That(table.Granted("rosa", TestDeclarations.World()), Is.Empty);
    }

    [Test]
    public void UnaTabellaCheNonSiPuoLeggereLoDice()
    {
        var missing = PositionTable.Load(TestDeclarations.FixturePath("non_c_e.json"));

        Assert.That(missing.IsOk, Is.False);
        Assert.That(missing.Code, Is.EqualTo("positions_not_found"));

        var broken = PositionTable.FromJson("[");
        Assert.That(broken.IsOk, Is.False);
        Assert.That(broken.Code, Is.EqualTo("invalid_positions"));
    }
}
