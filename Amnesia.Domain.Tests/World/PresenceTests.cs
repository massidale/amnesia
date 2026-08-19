using Amnesia.Core;
using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.World;

public class PresenceTests
{
    private static WorldState EmptyWorld()
    {
        var world = new WorldState();
        world.ActorOf("system");
        world.ActorOf("player");
        world.ActorOf("anna");
        world.ActorOf("matteo");
        return world;
    }

    [Test]
    public void SenzaPosizioneSonoTuttiPresenti()
    {
        var map = TestVillage.Map();
        var world = EmptyWorld();

        Assert.That(Presence.HasPosition(world, "anna"), Is.False, "un attore nasce senza posizione");
        Assert.That(Presence.PositionOf(world, "anna"), Is.Null);
        Assert.That(Presence.CoLocated(world, "anna", "matteo", map), Is.True, "senza posizioni sono tutti presenti — il gioco testuale non ha mappa");
    }

    /// Il caso asimmetrico e' quello che regge tutto: le interfacce testuali non
    /// posizionano nessuno, e il sistema di movimento posiziona alcuni attori e
    /// non altri.
    [Test]
    public void UnoPosizionatoEUnoNoContanoComunqueComePresenti()
    {
        var map = TestVillage.Map();
        var world = EmptyWorld();
        Presence.SetPosition(world, "anna", TestVillage.Center(map, "cucina"));

        Assert.That(Presence.CoLocated(world, "anna", "matteo", map), Is.True);
        Assert.That(Presence.CoLocated(world, "matteo", "anna", map), Is.True, "e la regola e' simmetrica");
    }

    [Test]
    public void LaPosizioneSiRilegge()
    {
        var map = TestVillage.Map();
        var world = EmptyWorld();
        Presence.SetPosition(world, "anna", TestVillage.Center(map, "cucina"));

        Assert.That(Presence.HasPosition(world, "anna"), Is.True);
        Assert.That(TestVillage.PositionOf(world, "anna"), Is.EqualTo(TestVillage.Center(map, "cucina")));
        Assert.That(Presence.PlaceOf(world, "anna", map), Is.EqualTo("cucina"), "il luogo si ricava dalla cella");
    }

    /// La presenza e' una stanza, non una piastrella: il sistema di movimento si
    /// ferma sulla prima cella della destinazione che incontra, che non e' quasi
    /// mai la cella su cui sta un altro.
    [Test]
    public void LaStessaStanzaEDentroLoStessoLuogoAncheSuCelleDiverse()
    {
        var map = TestVillage.Map();
        var world = EmptyWorld();
        Presence.SetPosition(world, "anna", TestVillage.Center(map, "sala"));
        Presence.SetPosition(world, "matteo", TestVillage.Center(map, "cucina"));
        Assert.That(Presence.CoLocated(world, "anna", "matteo", map), Is.False, "due stanze attigue non sono lo stesso luogo");

        Presence.SetPosition(world, "matteo", TestVillage.Center(map, "sala"));
        Assert.That(Presence.CoLocated(world, "anna", "matteo", map), Is.True, "la stessa cella e' lo stesso luogo");

        var elsewhereInSala = new Cell(6, 3);
        Assert.That(map.PlaceAt(elsewhereInSala), Is.EqualTo("sala"), "la seconda cella e' nella stessa stanza");
        Assert.That(elsewhereInSala, Is.Not.EqualTo(TestVillage.Center(map, "sala")), "e non e' la cella su cui sta l'altro");
        Presence.SetPosition(world, "matteo", elsewhereInSala);
        Assert.That(Presence.CoLocated(world, "anna", "matteo", map), Is.True, "la stessa stanza e' lo stesso luogo, senza esattezza di piastrella");
    }

    /// Una soglia non appartiene a nessuna stanza, e un luogo vuoto non e' un luogo.
    [Test]
    public void DueSoglieNonSonoLoStessoLuogo()
    {
        var map = TestVillage.Map();
        var world = EmptyWorld();
        var interiorDoor = new Cell(5, 4);
        var doorOntoTheLane = new Cell(3, 6);
        Assert.That(map.PlaceAt(interiorDoor), Is.EqualTo(""), "la porta interna non e' in nessuna stanza");
        Assert.That(map.PlaceAt(doorOntoTheLane), Is.EqualTo(""), "e nemmeno quella che da' sulla strada");

        Presence.SetPosition(world, "anna", interiorDoor);
        Assert.That(Presence.PlaceOf(world, "anna", map), Is.EqualTo(""), "chi sta su una soglia non e' in nessun luogo");
        Assert.That(Presence.HasPosition(world, "anna"), Is.True, "il che e' comunque una posizione");

        Presence.SetPosition(world, "matteo", doorOntoTheLane);
        Assert.That(Presence.CoLocated(world, "anna", "matteo", map), Is.False, "due soglie diverse non sono lo stesso luogo");
        Presence.SetPosition(world, "matteo", interiorDoor);
        Assert.That(Presence.CoLocated(world, "anna", "matteo", map), Is.False, "e nemmeno la stessa soglia — un luogo vuoto non e' un luogo");
    }

    [Test]
    public void LePosizioniSopravvivonoAlGiroSulDisco()
    {
        var map = TestVillage.Map();
        var world = EmptyWorld();
        Presence.SetPosition(world, "anna", TestVillage.Center(map, "cucina"));

        var restored = WorldState.FromJson(world.ToJson());

        Assert.That(TestVillage.PositionOf(restored, "anna"), Is.EqualTo(TestVillage.Center(map, "cucina")));
        Assert.That(Presence.HasPosition(restored, "matteo"), Is.False, "e chi non l'aveva non se la ritrova addosso");
    }
}
