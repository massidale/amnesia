using Amnesia.Core;
using Amnesia.Game;
using NUnit.Framework;

namespace Amnesia.Tests.Game;

public class PlaceServiceTests
{
    [Test]
    public void AprireIn3DNonRaccoglieIlContenuto()
    {
        var world = ConLaChiave();
        new Register(world).Record("anna", "magazzino_dove");
        var service = new PlaceService(Luoghi());
        var result = service.Apri(world, "magazzino_b17", raccogliContenuto: false);
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Value.Presi, Is.Empty);
        Assert.That(world.ItemOwners.ContainsKey("cassetta_latta"), Is.False);
    }

    [Test]
    public void LaRaccoltaESingolaEValidata()
    {
        var world = ConLaChiave();
        var service = new PlaceService(Luoghi());
        Assert.That(service.Raccogli(world, "magazzino_b17", "cassetta_latta").IsOk, Is.False);
        new Register(world).Record("anna", "magazzino_dove");
        service.Apri(world, "magazzino_b17", raccogliContenuto: false);
        Assert.That(service.Raccogli(world, "magazzino_b17", "inventato").IsOk, Is.False);
        Assert.That(service.Raccogli(world, "magazzino_b17", "cassetta_latta").IsOk, Is.True);
        Assert.That(world.ItemOwners.ContainsKey("braccialetto"), Is.False);
        Assert.That(service.Raccogli(world, "magazzino_b17", "cassetta_latta").IsOk, Is.False);
        world.ItemOwners["braccialetto"] = "nino";
        Assert.That(service.Raccogli(world, "magazzino_b17", "braccialetto").IsOk, Is.False);
        Assert.That(world.ItemOwners["braccialetto"], Is.EqualTo("nino"));
        var ripristinato = world.Clone();
        Assert.That(service.IsOpen(ripristinato, "magazzino_b17"), Is.True);
        Assert.That(service.Raccogli(ripristinato, "magazzino_b17", "cassetta_latta").IsOk, Is.False);
        Assert.That(ripristinato.ItemOwners["cassetta_latta"], Is.EqualTo("player"));
    }

    private static PlaceTable Luoghi() => PlaceTable.FromJson("""
    {"places": {"magazzino_b17": {
      "door": {"x": 39, "y": 28},
      "closed": "Una saracinesca di lamiera.",
      "opened": "La serratura e' dura ma gira.",
      "requires_item": "chiave_b17",
      "requires_declared": ["magazzino_dove"],
      "contains": ["cassetta_latta", "braccialetto"]
    }}}
    """).Value;

    private static WorldState ConLaChiave()
    {
        var world = new WorldState();
        world.ItemOwners["chiave_b17"] = "player";
        return world;
    }

    /// Il primo atto in una prova: la chiave ce l'ha dal risveglio, e per un
    /// atto intero non gli serve a niente perche' non sa dove sia la porta.
    [Test]
    public void LaChiaveDaSolaNonApreNiente()
    {
        var esito = new PlaceService(Luoghi()).Apri(ConLaChiave(), "magazzino_b17");

        Assert.That(esito.IsOk, Is.False);
        Assert.That(esito.Code, Is.EqualTo("needs_knowledge"));
        Assert.That(esito.Message, Is.EqualTo("magazzino_dove"));
    }

    [Test]
    public void SapereDoveSenzaLaChiaveNonBasta()
    {
        var world = new WorldState();
        new Register(world).Record("anna", "magazzino_dove");

        var esito = new PlaceService(Luoghi()).Apri(world, "magazzino_b17");

        Assert.That(esito.Code, Is.EqualTo("needs_item"));
        Assert.That(esito.Message, Is.EqualTo("chiave_b17"));
    }

    /// Una bocca sola basta: sapere dove sta una porta non e' una cosa da
    /// dimostrare, e la regola dei due sostegni serve a dimostrare.
    [Test]
    public void UnaBoccaSolaBastaAFartiSapereDove()
    {
        var world = ConLaChiave();
        new Register(world).Record("anna", "magazzino_dove");
        Assert.That(new Register(world).IsEstablished("magazzino_dove"), Is.False, "non e' stabilita, e va bene cosi'");

        var esito = new PlaceService(Luoghi()).Apri(world, "magazzino_b17");

        Assert.That(esito.IsOk, Is.True, esito.Message);
        Assert.That(esito.Value.Presi, Is.EqualTo(new[] { "cassetta_latta", "braccialetto" }));
        Assert.That(world.ItemOwners["braccialetto"], Is.EqualTo("player"));
        Assert.That(esito.Value.Racconto, Does.StartWith("La serratura"));
    }

    [Test]
    public void UnMagazzinoSiSvuotaUnaVoltaSola()
    {
        var world = ConLaChiave();
        new Register(world).Record("anna", "magazzino_dove");
        var porte = new PlaceService(Luoghi());
        porte.Apri(world, "magazzino_b17");

        var seconda = porte.Apri(world, "magazzino_b17");

        Assert.That(seconda.Code, Is.EqualTo("already_open"));
        Assert.That(porte.IsOpen(world, "magazzino_b17"), Is.True);
    }

    /// Le chiavi che nessuno legge sono la classe di errore piu' cara che ci
    /// sia: il lucchetto non scatta, il gioco parte, e te ne accorgi la sera in
    /// cui un giocatore resta chiuso fuori.
    [Test]
    public void UnaChiaveCheNessunoLeggeFermaIlCaricamento()
    {
        var caricata = PlaceTable.FromJson("""
        {"places": {"magazzino_b17": {"requires_declarated": ["magazzino_dove"]}}}
        """);

        Assert.That(caricata.IsOk, Is.False);
        Assert.That(caricata.Message, Does.Contain("requires_declarated"));
    }

    [Test]
    public void LaPortaSiTrovaDallaCella()
    {
        Assert.That(Luoghi().PlaceAtDoor(new Cell(39, 28)), Is.EqualTo("magazzino_b17"));
        Assert.That(Luoghi().PlaceAtDoor(new Cell(1, 1)), Is.Empty);
    }
}
