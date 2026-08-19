using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Game;
using NUnit.Framework;

namespace Amnesia.Tests.Game;

public class TaccuinoTests
{
    private static ItemCatalog Catalogo() => new(
        new ItemDefinition("fotografia", "una fotografia sbiadita", "la fotografia"),
        new ItemDefinition("braccialetto", "un braccialetto d'argento", "il braccialetto"),
        new ItemDefinition("chiave_b17", "una chiave d'ottone", "la chiave"));

    private static DeclarationTable Tabella() => DeclarationTable.FromJson("""
    {"declarations": {
      "versione_paese": {"text": "E per quella creatura e' stato un attimo.", "truth": "impossibile", "sources": ["rosa", "nino"]},
      "anna_solo_matteo": {"text": "Alla cava era rimasto solo Matteo.", "truth": "vera", "sources": ["anna"]}
    }}
    """).Value;

    [Test]
    public void LeTascheContengonoSoloCioCheIlGiocatoreHa()
    {
        var world = new WorldState();
        world.ItemOwners["fotografia"] = "player";
        world.ItemOwners["braccialetto"] = "matteo";

        var tasche = new Taccuino(world, Tabella(), Catalogo()).Tasche();

        Assert.That(tasche.Select(oggetto => oggetto.Id), Is.EqualTo(new[] { "fotografia" }));
    }

    [Test]
    public void LeTascheSeguonoLOrdineDelCatalogoENonQuelloDelDizionario()
    {
        var world = new WorldState();
        world.ItemOwners["chiave_b17"] = "player";
        world.ItemOwners["fotografia"] = "player";

        var tasche = new Taccuino(world, Tabella(), Catalogo()).Tasche();

        Assert.That(tasche.Select(oggetto => oggetto.Id), Is.EqualTo(new[] { "fotografia", "chiave_b17" }));
    }

    [Test]
    public void LeRigheSonoCitazioniAttribuiteNellOrdineInCuiSonoArrivate()
    {
        var world = new WorldState();
        var registro = new Register(world);
        registro.Record("anna", "anna_solo_matteo");
        registro.Record("rosa", "versione_paese");
        registro.Record("nino", "versione_paese");
        registro.Record("rosa", "versione_paese");

        var righe = new Taccuino(world, Tabella(), Catalogo()).Dette();

        Assert.That(righe.Select(riga => riga.Id), Is.EqualTo(new[] { "anna_solo_matteo", "versione_paese" }));
        Assert.That(righe[0].Testo, Is.EqualTo("Alla cava era rimasto solo Matteo."));
        Assert.That(righe[1].Bocche, Is.EqualTo(new[] { "rosa", "nino" }), "il coro conta le bocche, in ordine di arrivo");
        Assert.That(righe[1].Volte, Is.EqualTo(3), "e le ripetizioni, che sono il collante del quarto atto");
    }

    /// La prova che tiene in piedi la difficolta' del gioco: sette dichiarazioni
    /// su trentotto sono false, dette da gente che ci crede, e il taccuino non
    /// deve poter distinguere le une dalle altre nemmeno per sbaglio.
    [Test]
    public void IlTaccuinoNonDiceMaiSeUnaCosaEVera()
    {
        var world = new WorldState();
        new Register(world).Record("rosa", "versione_paese");

        var riga = new Taccuino(world, Tabella(), Catalogo()).Dette().Single();

        Assert.That(
            typeof(RigaDelTaccuino).GetProperties().Select(p => p.Name),
            Has.No.Member("Truth").And.No.Member("Verita"),
            "la colonna della verita' esiste nei dati e non deve uscire di li'");
        Assert.That(riga.Testo, Does.Not.Contain("impossibile"));
    }
}
