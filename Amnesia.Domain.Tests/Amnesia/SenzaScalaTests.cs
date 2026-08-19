using Amnesia;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Chi non ha una scala non e' un personaggio a cui manca un pezzo: e' uno che
/// non nasconde niente. Rosa, Nino, Wanda. Se la scala fosse l'unico permesso,
/// resterebbero muti attraverso lo strumento — l'esatto contrario di cio' che
/// sono, e il tipo di guasto che non si vede finche' non ci si gioca.
public class SenzaScalaTests
{
    private static DeclarationService Service() =>
        new(TestDeclarations.Table(), TestDeclarations.Positions());

    [Test]
    public void ChiNonHaUnaScalaPuoDireCioDiCuiEFonte()
    {
        var world = TestDeclarations.World();

        var declared = Service().Declare(world, "rosa", "usciva_allegro");

        Assert.That(declared.IsOk, Is.True, "Rosa non ha una scala e non ha niente da tenersi");
        Assert.That(new Register(world).SupportsFor("usciva_allegro"), Does.Contain("rosa"));
    }

    [Test]
    public void MaLaPrecondizioneValeAnchePerLei()
    {
        // Non avere una scala non e' una deroga alle precondizioni: il gruppo lo
        // ammette chiunque, ma davanti alla fotografia. Senza, quella riga non e'
        // ancora sua.
        var world = TestDeclarations.World();

        Assert.That(Service().Declare(world, "rosa", "circolo_esisteva").IsOk, Is.False,
            "a freddo la fotografia non gliel'ha messa davanti nessuno");

        world.MarkShown("rosa", "fotografia");

        Assert.That(Service().Declare(world, "rosa", "circolo_esisteva").IsOk, Is.True);
        Assert.That(new Register(world).SupportsFor("circolo_esisteva"), Does.Contain("rosa"));
    }

    [Test]
    public void MaNonPuoDireCioDiCuiNonEFonte()
    {
        var world = TestDeclarations.World();

        var declared = Service().Declare(world, "rosa", "scampagnate");

        Assert.That(declared.IsOk, Is.False, "non essere guardinghi non vuol dire sapere tutto");
        Assert.That(declared.Code, Is.EqualTo("declaration_not_granted"));
    }

    [Test]
    public void EUnoSconosciutoNonPuoDireNiente()
    {
        var declared = Service().Declare(TestDeclarations.World(), "nessuno", "circolo_esisteva");

        Assert.That(declared.IsOk, Is.False);
    }

    [Test]
    public void ChiHaUnaScalaRestaVincolatoAllaScala()
    {
        var cold = TestDeclarations.World();

        var declared = Service().Declare(cold, "matteo", "non_erano_gite");

        Assert.That(declared.IsOk, Is.False,
            "la deroga vale per chi non ha una scala, non per chi ce l'ha e non e' salito");
    }
}
