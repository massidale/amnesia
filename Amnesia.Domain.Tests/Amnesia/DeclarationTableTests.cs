using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

public class DeclarationTableTests
{
    [Test]
    public void LaTabellaSaCosaContieneECosaNo()
    {
        var table = TestDeclarations.Table();

        Assert.That(table.Has("scampagnate"), Is.True);
        Assert.That(table.Has("non_esiste"), Is.False, "e non contiene quello che non c'e'");
    }

    [Test]
    public void IlTestoCanonicoEQuelloScrittoNeiDati()
    {
        Assert.That(TestDeclarations.Table().TextOf("scampagnate"), Does.StartWith("Era una compagnia"));
    }

    [Test]
    public void UnaDichiarazioneSaDaQualiBocchePuoUscire()
    {
        Assert.That(TestDeclarations.Table().SourcesOf("circolo_esisteva"),
            Is.EqualTo(new[] { "rosa", "matteo" }), "circolo_esisteva ha due fonti");
    }

    [Test]
    public void UnaPrecondizioneEUnaListaDiCoseCheGliVannoMesseDavanti()
    {
        var table = TestDeclarations.Table();

        Assert.That(table.RequiresShown("non_erano_gite"), Is.EqualTo(new[] { "frase" }));
        Assert.That(table.RequiresShown("scampagnate"), Is.Empty, "una dichiarazione senza precondizione non ne dichiara");
    }

    [Test]
    public void UnIdSconosciutoNonFaEsplodereNiente()
    {
        // La tabella e' dati, e i dati si sbagliano: un refuso costa una riga
        // mancante, non una partita interrotta. Decide il chiamante.
        var table = TestDeclarations.Table();

        Assert.That(table.TextOf("non_esiste"), Is.Empty);
        Assert.That(table.SourcesOf("non_esiste"), Is.Empty);
        Assert.That(table.RequiresShown("non_esiste"), Is.Empty);
        Assert.That(table.Find("non_esiste"), Is.Null);
    }

    [Test]
    public void UnaTabellaCheNonSiPuoLeggereLoDiceInveceDiRenderneUnaVuota()
    {
        // In GDScript un file mancante era un push_error e una tabella vuota, cioe'
        // ogni personaggio muto e nessuno che sapesse perche'.
        var missing = DeclarationTable.Load(TestDeclarations.FixturePath("non_c_e.json"));

        Assert.That(missing.IsOk, Is.False);
        Assert.That(missing.Code, Is.EqualTo("declarations_not_found"));
    }

    [Test]
    public void UnaTabellaMalformataVieneRifiutata()
    {
        var broken = DeclarationTable.FromJson("non sono json");

        Assert.That(broken.IsOk, Is.False);
        Assert.That(broken.Code, Is.EqualTo("invalid_declarations"));
    }
}
