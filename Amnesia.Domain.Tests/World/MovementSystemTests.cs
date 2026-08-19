using Amnesia.Core;
using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.World;

public class MovementSystemTests
{
    private VillageMap _map = null!;
    private MovementSystem _system = null!;

    [SetUp]
    public void SetUp()
    {
        _map = TestVillage.Map();
        _system = new MovementSystem(_map, new Navigator(_map), TestVillage.Routines());
    }

    private WorldState World() => TestVillage.World(_map);

    private string PlaceOf(WorldState world, string npcId) => Presence.PlaceOf(world, npcId, _map);

    [Test]
    public void ChiEGiaDoveLaRoutineLoVuoleNonGirovaga()
    {
        var world = World();
        var before = TestVillage.PositionOf(world, "anna");

        _system.Advance(world, 5.0);

        Assert.That(TestVillage.PositionOf(world, "anna"), Is.EqualTo(before));
    }

    /// Il giocatore e' affare del giocatore: nemmeno un incarico esplicito gli
    /// muove i piedi.
    [Test]
    public void IlSistemaDiMovimentoNonMuoveMaiIlGiocatore()
    {
        var world = World();
        var before = TestVillage.PositionOf(world, "player");
        _system.SetIntent(world, "player", "cava");

        _system.Advance(world, 30.0);

        Assert.That(TestVillage.PositionOf(world, "player"), Is.EqualTo(before));
    }

    [Test]
    public void AlleTrediciLaRoutineDiBrunoLoPortaInStradaSenzaCheNessunoGlieloChieda()
    {
        var world = World();
        world.Minute = 780;

        _system.Advance(world, 5.0);

        Assert.That(PlaceOf(world, "bruno"), Is.EqualTo("strada"));
        Assert.That(_system.IntentOf(world, "bruno"), Is.EqualTo(""), "e non c'e' voluto nessun incarico");
    }

    [Test]
    public void UnIncaricoBatteLaRoutineECiSiVaCamminando()
    {
        var world = World();
        var kitchen = TestVillage.Center(_map, "cucina");
        _system.SetIntent(world, "anna", "bottega");
        Assert.That(_system.IntentOf(world, "anna"), Is.EqualTo("bottega"), "l'incarico e' registrato");

        _system.Advance(world, 0.25);

        var stepped = TestVillage.PositionOf(world, "anna");
        Assert.That(stepped, Is.Not.EqualTo(kitchen), "si e' messa in cammino");
        Assert.That(_map.IsWalkable(stepped), Is.True, "e non mette mai un piede in un muro");
        Assert.That(PlaceOf(world, "anna"), Is.Not.EqualTo("bottega"), "ma non e' ancora arrivata");

        _system.Advance(world, 60.0);

        Assert.That(PlaceOf(world, "anna"), Is.EqualTo("bottega"), "con abbastanza tempo arriva");
    }

    /// Arrivare non chiude l'incarico: la commissione finisce quando lo dice chi
    /// l'ha data, non quando si fermano i piedi. La routine la vuole in cucina, e
    /// non deve poterla portare via dalla scena.
    [Test]
    public void ArrivareNonCancellaLIncaricoELaRoutineNonSeLaRiprende()
    {
        var world = World();
        _system.SetIntent(world, "anna", "bottega");
        _system.Advance(world, 60.0);
        Assert.That(PlaceOf(world, "anna"), Is.EqualTo("bottega"));
        Assert.That(_system.IntentOf(world, "anna"), Is.EqualTo("bottega"), "arrivare lascia l'incarico dov'era");

        var arrived = TestVillage.PositionOf(world, "anna");
        _system.Advance(world, 120.0);

        Assert.That(TestVillage.PositionOf(world, "anna"), Is.EqualTo(arrived), "due ore dopo e' ancora li', non tornata ai fornelli");
        Assert.That(PlaceOf(world, "anna"), Is.EqualTo("bottega"));

        _system.ClearIntent(world, "anna");
        Assert.That(_system.IntentOf(world, "anna"), Is.EqualTo(""), "la commissione la chiude chi l'ha data");

        _system.Advance(world, 60.0);

        Assert.That(PlaceOf(world, "anna"), Is.EqualTo("cucina"), "e solo allora la routine se la riprende");
    }

    /// Il movimento e' funzione dei minuti di gioco, non di quante volte lo si
    /// chiama.
    [Test]
    public void GliStessiMinutiInUnaChiamataOInOtteFinisconoSullaStessaCella()
    {
        var oneGo = World();
        _system.SetIntent(oneGo, "anna", "bottega");
        _system.Advance(oneGo, 1.0);

        var sliced = World();
        _system.SetIntent(sliced, "anna", "bottega");
        for (var index = 0; index < 8; index++)
        {
            _system.Advance(sliced, 0.125);
        }

        Assert.That(PlaceOf(oneGo, "anna"), Is.Not.EqualTo("bottega"), "un minuto non basta ad arrivare");
        Assert.That(TestVillage.PositionOf(sliced, "anna"), Is.EqualTo(TestVillage.PositionOf(oneGo, "anna")), "un minuto in una chiamata o in otto, stessa cella");

        var twoInOne = World();
        _system.SetIntent(twoInOne, "anna", "bottega");
        _system.Advance(twoInOne, 2.0);
        var twoInEight = World();
        _system.SetIntent(twoInEight, "anna", "bottega");
        for (var index = 0; index < 8; index++)
        {
            _system.Advance(twoInEight, 0.25);
        }

        Assert.That(TestVillage.PositionOf(twoInEight, "anna"), Is.EqualTo(TestVillage.PositionOf(twoInOne, "anna")), "due minuti in una chiamata o in otto, stessa cella");
    }

    /// Lo stesso attraverso un arrivo e su una tratta lunga: un'ora in una
    /// chiamata, in due o in ventiquattro deve lasciarla sulla medesima cella.
    [Test]
    public void UnOraInUnaChiamataInDueOInVentiquattroFiniscePerLoStessoVerso()
    {
        var whole = World();
        _system.SetIntent(whole, "anna", "bottega");
        _system.Advance(whole, 60.0);

        var halves = World();
        _system.SetIntent(halves, "anna", "bottega");
        _system.Advance(halves, 30.0);
        _system.Advance(halves, 30.0);

        var many = World();
        _system.SetIntent(many, "anna", "bottega");
        for (var index = 0; index < 24; index++)
        {
            _system.Advance(many, 2.5);
        }

        Assert.That(TestVillage.PositionOf(halves, "anna"), Is.EqualTo(TestVillage.PositionOf(whole, "anna")), "un'ora in una chiamata o in due, stessa cella");
        Assert.That(TestVillage.PositionOf(many, "anna"), Is.EqualTo(TestVillage.PositionOf(whole, "anna")), "un'ora in ventiquattro chiamate, di nuovo la stessa cella");
        Assert.That(PlaceOf(many, "anna"), Is.EqualTo(PlaceOf(whole, "anna")), "e lo stesso luogo");
    }

    /// Una camminata che nessuno completa, in mezzo all'aperto, dove fra due celle
    /// ci sono decine di vie ugualmente corte. La cella e' scritta qui a mano: se
    /// il criterio di pareggio dell'A* cambia, i minuti sono gli stessi ma la
    /// camminata no, e questo e' l'unico posto che se ne accorge.
    [Test]
    public void AMetaStradaLeFetteSeguonoLeStesseCelleELaViaEQuellaFissata()
    {
        var oneGo = World();
        Presence.SetPosition(oneGo, "anna", TestVillage.Center(_map, "strada"));
        _system.SetIntent(oneGo, "anna", "cava");
        _system.Advance(oneGo, 0.25);

        var sliced = World();
        Presence.SetPosition(sliced, "anna", TestVillage.Center(_map, "strada"));
        _system.SetIntent(sliced, "anna", "cava");
        for (var index = 0; index < 4; index++)
        {
            _system.Advance(sliced, 0.0625);
        }

        Assert.That(TestVillage.PositionOf(sliced, "anna"), Is.EqualTo(TestVillage.PositionOf(oneGo, "anna")), "in mezzo al paese la camminata affettata segue le stesse celle di quella intera");
        Assert.That(TestVillage.PositionOf(oneGo, "anna"), Is.EqualTo(new Cell(6, 9)), "e sono queste celle, non altre ugualmente corte");
    }

    /// La fetta e' ricavata dal passo di cammino invece di essere scritta come
    /// numero, cosi' resta otto decimi di cella qualunque sia il passo.
    private const double SubCellMinutes = 0.8 / MovementSystem.WalkCellsPerMinute;

    [Test]
    public void IlRestoDiUnPassoSopravviveAlSalvataggio()
    {
        var world = World();
        var kitchen = TestVillage.Center(_map, "cucina");
        _system.SetIntent(world, "anna", "bottega");

        _system.Advance(world, SubCellMinutes);

        Assert.That(TestVillage.PositionOf(world, "anna"), Is.EqualTo(kitchen), "otto decimi di cella non sono una cella, quindi non si e' ancora mossa");

        var resumed = WorldState.FromJson(world.ToJson());
        _system.Advance(resumed, SubCellMinutes);

        Assert.That(TestVillage.PositionOf(resumed, "anna"), Is.Not.EqualTo(kitchen), "gli otto decimi sono tornati su dal disco e hanno completato una cella");
    }

    [Test]
    public void IlRestoDiUnMondoNonSiVersaInUnAltro()
    {
        var mine = World();
        var yours = World();
        var kitchen = TestVillage.Center(_map, "cucina");
        _system.SetIntent(mine, "anna", "bottega");
        _system.SetIntent(yours, "anna", "bottega");

        _system.Advance(mine, SubCellMinutes);
        _system.Advance(yours, SubCellMinutes);

        Assert.That(TestVillage.PositionOf(yours, "anna"), Is.EqualTo(kitchen), "il resto di un mondo non muove l'Anna di un altro");
    }

    [Test]
    public void UnaFettaNegativaNonMuoveNessunoENonAvvelenaIlResto()
    {
        var world = World();
        var kitchen = TestVillage.Center(_map, "cucina");
        _system.SetIntent(world, "anna", "bottega");

        _system.Advance(world, -0.03);
        Assert.That(TestVillage.PositionOf(world, "anna"), Is.EqualTo(kitchen), "una fetta negativa non muove nessuno");
        _system.Advance(world, 0.0);
        Assert.That(TestVillage.PositionOf(world, "anna"), Is.EqualTo(kitchen), "e nemmeno una fetta nulla");

        _system.Advance(world, 0.125);
        Assert.That(TestVillage.PositionOf(world, "anna"), Is.Not.EqualTo(kitchen), "e le due celle intere che seguono avvengono comunque");
    }

    [Test]
    public void GliIncarichiSopravvivonoAlSalvataggio()
    {
        var world = World();
        _system.SetIntent(world, "matteo", "cava");

        var reloaded = WorldState.FromJson(world.ToJson());

        Assert.That(_system.IntentOf(reloaded, "matteo"), Is.EqualTo("cava"));
        Assert.That(_system.IntentOf(reloaded, "anna"), Is.EqualTo(""));
    }

    [Test]
    public void UnaDestinazioneCheNonEsisteSiFaSentireSubito()
    {
        var world = World();
        Assert.That(() => _system.SetIntent(world, "anna", "cattedrale"), Throws.ArgumentException);
        Assert.That(_system.IntentOf(world, "anna"), Is.EqualTo(""), "e non resta un incarico impossibile addosso a nessuno");
    }

    [Test]
    public void ChiNonHaPosizioneNonVieneMessoSullaMappaDalMovimento()
    {
        var world = World();
        world.ActorOf("carla");
        _system.SetIntent(world, "carla", "cava");

        _system.Advance(world, 60.0);

        Assert.That(Presence.HasPosition(world, "carla"), Is.False, "chi non e' sulla mappa non ci finisce camminando");
        Assert.That(Presence.HasPosition(world, "system"), Is.False, "e il narratore non ha piedi");
    }
}
