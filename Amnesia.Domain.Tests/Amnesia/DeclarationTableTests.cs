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
        var table = TestDeclarations.Table();

        Assert.That(table.SourcesOf("circolo_esisteva"), Does.Contain("rosa").And.Contain("matteo"),
            "il gruppo lo puo' ammettere chiunque, davanti alla fotografia");
        Assert.That(table.SourcesOf("affitto"), Is.EqualTo(new[] { "rosa" }),
            "e c'e' chi ha una bocca sola: l'affitto lo sa solo la moglie che l'ha visto pagare");
        Assert.That(table.SourcesOf("scampagnate"), Does.Not.Contain("rosa"),
            "la copertura la dicono i membri, non chi non c'era");
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
