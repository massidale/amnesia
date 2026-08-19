using Amnesia.Core;
using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.World;

public class NavigatorTests
{
    private static readonly string[] Places = { "cucina", "sala", "bottega", "strada", "cava" };

    [Test]
    public void OgniLuogoSiRaggiungeDaOgniAltro()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);
        foreach (var a in Places)
        {
            foreach (var b in Places)
            {
                if (a == b)
                {
                    continue;
                }
                Assert.That(nav.Reachable(TestVillage.Center(map, a), TestVillage.Center(map, b)), Is.True, $"{a} raggiunge {b}");
            }
        }
    }

    [Test]
    public void IlPercorsoEsceDallaCellaDiPartenzaEArrivaADestinazioneSenzaAttraversareMuri()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);
        var start = TestVillage.Center(map, "cucina");
        var goal = TestVillage.Center(map, "bottega");

        var route = nav.Path(start, goal);

        Assert.That(route, Is.Not.Empty, "una via fra cucina e bottega esiste");
        Assert.That(route[route.Count - 1], Is.EqualTo(goal), "il percorso finisce a destinazione");
        Assert.That(route.All(map.IsWalkable), Is.True, "il percorso non attraversa mai un muro");
        // Un passo alla volta e mai in diagonale: cosi' una cella di cammino costa
        // uguale in ogni direzione, e i minuti di un percorso sono la sua lunghezza.
        var previous = start;
        foreach (var cell in route)
        {
            var distance = Math.Abs(cell.X - previous.X) + Math.Abs(cell.Y - previous.Y);
            Assert.That(distance, Is.EqualTo(1), "ogni passo e' una sola cella ortogonale");
            previous = cell;
        }
        Assert.That(route[0], Is.Not.EqualTo(start), "la cella di partenza non fa parte del percorso");
    }

    /// La lunghezza e' fissata perche' le celle diventano minuti: un percorso che
    /// cresce o si accorcia in silenzio e' un orario che slitta in silenzio.
    [Test]
    public void LaLunghezzaDelPercorsoEFissata()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);
        Assert.That(nav.Path(TestVillage.Center(map, "cucina"), TestVillage.Center(map, "bottega")).Count, Is.EqualTo(19));
    }

    [Test]
    public void LaStessaDomandaDaLaStessaRispostaAncheDaUnAltroNavigatore()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);
        var route = nav.Path(TestVillage.Center(map, "cucina"), TestVillage.Center(map, "bottega"));

        Assert.That(nav.Path(TestVillage.Center(map, "cucina"), TestVillage.Center(map, "bottega")), Is.EqualTo(route), "richiedere due volte da lo stesso percorso");

        var otherMap = TestVillage.Map();
        var otherNav = new Navigator(otherMap);
        Assert.That(
            otherNav.Path(TestVillage.Center(otherMap, "cucina"), TestVillage.Center(otherMap, "bottega")),
            Is.EqualTo(route),
            "un Navigator costruito a parte da lo stesso percorso, cella per cella");
    }

    /// Il cuore di tutto: fra strada e cava e' tutto aperto, e le vie ugualmente
    /// corte sono decine. Quale viene scelta non e' un dettaglio interno — e' la
    /// camminata che il giocatore vede — e deve dipendere solo dalle due celle,
    /// mai dall'ordine in cui la coda ha ricevuto i nodi. Se questo percorso
    /// cambia perche' e' cambiato il criterio di pareggio, e' cambiato il gioco.
    [Test]
    public void IlPareggioSiRompeSempreAllaStessaManiera()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);

        var route = nav.Path(TestVillage.Center(map, "strada"), TestVillage.Center(map, "cava"));

        Assert.That(route, Is.EqualTo(new[]
        {
            new Cell(9, 9), new Cell(8, 9), new Cell(7, 9), new Cell(6, 9),
            new Cell(5, 9), new Cell(4, 9), new Cell(4, 10), new Cell(4, 11), new Cell(4, 12),
        }));
    }

    /// La proprieta' su cui poggia tutto il movimento: la cella successiva di un
    /// percorso minimo e' funzione pura della cella su cui si sta, percio' fare un
    /// passo e richiedere il percorso da capo da' esattamente il resto della via.
    /// E' verificata, non data per buona: con un altro criterio di pareggio
    /// smetterebbe di valere e nessuno se ne accorgerebbe finche' un personaggio
    /// non comincia a oscillare fra due celle.
    [Test]
    public void UnPassoAllaVoltaSegueLaStessaViaDiUnPercorsoIntero()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);
        foreach (var place in Places)
        {
            var goal = TestVillage.Center(map, place);
            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    var from = new Cell(x, y);
                    if (!map.IsWalkable(from) || from == goal)
                    {
                        continue;
                    }
                    var route = nav.Path(from, goal);
                    if (route.Count == 0)
                    {
                        continue;
                    }
                    Assert.That(nav.Path(route[0], goal), Is.EqualTo(route.Skip(1).ToList()), $"da {from} verso {place}");
                }
            }
        }
    }

    [Test]
    public void NonCEPercorsoVersoDoveSiStaGiaNeDentroUnMuro()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);
        var shop = TestVillage.Center(map, "bottega");
        Assert.That(nav.Path(shop, shop), Is.Empty, "nessun percorso verso dove si sta gia'");
        Assert.That(nav.Path(shop, new Cell(1, 3)), Is.Empty, "nessun percorso dentro un muro");
        Assert.That(nav.Path(new Cell(1, 3), shop), Is.Empty, "e nessuno da dentro un muro");
        Assert.That(nav.Path(shop, new Cell(-9, -9)), Is.Empty, "nessun percorso fuori dalla mappa");
    }

    [Test]
    public void UnMuroNonERaggiungibileNemmenoDaSeStesso()
    {
        var map = TestVillage.Map();
        var nav = new Navigator(map);
        Assert.That(nav.Reachable(new Cell(1, 3), new Cell(1, 3)), Is.False, "in un muro non ci si sta");
        Assert.That(nav.Reachable(new Cell(-9, -9), new Cell(-9, -9)), Is.False, "e fuori dalla mappa nemmeno");
        var shop = TestVillage.Center(map, "bottega");
        Assert.That(nav.Reachable(shop, shop), Is.True, "dove si sta gia' e' raggiungibile");
    }
}
