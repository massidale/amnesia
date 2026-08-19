using Amnesia.Core;
using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.World;

public class VillageMapTests
{
    private static readonly string[] Places = { "cucina", "sala", "bottega", "strada", "cava" };
    private static readonly string[] Actors = { "player", "anna", "matteo", "bruno" };

    [Test]
    public void LaMappaSiCaricaDaUnPercorsoSulDisco()
    {
        var map = TestVillage.Map();
        Assert.That(map.Width, Is.EqualTo(20));
        Assert.That(map.Height, Is.EqualTo(14));
    }

    /// width e height sono solo cio' che il file DICHIARA. Terrain() risponde
    /// niente per una colonna oltre la fine di una riga corta, percio' una riga
    /// ritoccata a mano a 19 caratteri sarebbe una striscia invisibile e non
    /// calpestabile che nessun'altra asserzione qui noterebbe — e un carattere
    /// fuori legenda e' un buco nel disegno del paese. Sono le due regressioni
    /// che questo formato invita, e si prendono un'asserzione loro.
    [Test]
    public void LeRigheSonoEsattamenteQuanteEQuantoIlFileDichiara()
    {
        var map = TestVillage.Map();
        Assert.That(map.Rows.Count, Is.EqualTo(map.Height), "il file ha esattamente height righe");

        var ragged = new List<int>();
        var strays = new SortedSet<char>();
        for (var y = 0; y < map.Rows.Count; y++)
        {
            var row = map.Rows[y];
            if (row.Length != map.Width)
            {
                ragged.Add(y);
            }
            foreach (var drawn in row)
            {
                if (!map.Legend.ContainsKey(drawn))
                {
                    strays.Add(drawn);
                }
            }
        }
        Assert.That(ragged, Is.Empty, "ogni riga e' lunga esattamente width");
        Assert.That(strays, Is.Empty, "ogni carattere disegnato sta in legenda");
    }

    [Test]
    public void OgniLuogoEsisteEdECalpestabileCellaPerCella()
    {
        var map = TestVillage.Map();
        foreach (var place in Places)
        {
            var cells = map.CellsOf(place);
            Assert.That(cells, Is.Not.Empty, $"il luogo {place} esiste");
            Assert.That(cells.All(map.IsWalkable), Is.True, $"ogni cella di {place} e' calpestabile");
            Assert.That(map.PlaceAt(TestVillage.Center(map, place)), Is.EqualTo(place), $"il centro di {place} gli appartiene");
        }
    }

    [Test]
    public void OgniAttoreNasceSuUnaCellaCalpestabile()
    {
        var map = TestVillage.Map();
        foreach (var actorId in Actors)
        {
            var spawn = map.Spawn(actorId);
            Assert.That(spawn, Is.Not.Null, $"{actorId} ha una cella di partenza");
            Assert.That(map.IsWalkable(spawn!.Value), Is.True, $"la partenza di {actorId} e' calpestabile");
        }
    }

    [Test]
    public void FuoriDallaMappaNonCENiente()
    {
        var map = TestVillage.Map();
        Assert.That(map.PlaceAt(new Cell(-1, -1)), Is.EqualTo(""), "una cella fuori mappa non appartiene a nessun luogo");
        Assert.That(map.IsWalkable(new Cell(-1, 0)), Is.False, "fuori dalla mappa non si cammina");
        Assert.That(map.IsWalkable(new Cell(20, 0)), Is.False, "oltre il bordo destro nemmeno");
        Assert.That(map.Terrain(new Cell(0, 14)), Is.Null, "e sotto il bordo non c'e' terreno");
    }

    [Test]
    public void OgniEdificioHaUnaPorta()
    {
        var map = TestVillage.Map();
        var doors = 0;
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (map.Terrain(new Cell(x, y)) == '+')
                {
                    doors++;
                }
            }
        }
        Assert.That(doors, Is.GreaterThanOrEqualTo(3), "ogni edificio ha una soglia");
    }

    [Test]
    public void UnLuogoCheNonEsisteNonHaNeCentroNeCelle()
    {
        var map = TestVillage.Map();
        Assert.That(map.HasPlace("cattedrale"), Is.False);
        Assert.That(map.CenterOf("cattedrale"), Is.Null, "niente, non la cella (0, 0) che e' una cella vera");
        Assert.That(map.CellsOf("cattedrale"), Is.Empty);
        Assert.That(map.Spawn("nessuno"), Is.Null);
    }

    /// Due rettangoli possono sovrapporsi, e allora vince il primo dichiarato.
    /// L'ordine di enumerazione di un dizionario non e' garantito da nessuna
    /// parte, e una mappa che risponde diversamente a seconda della build non
    /// e' una mappa.
    [Test]
    public void QuandoDueLuoghiSiSovrappongonoVinceIlPrimoDichiarato()
    {
        var loaded = VillageMap.FromJson("""
        {
          "width": 4, "height": 2,
          "legend": {".": true},
          "rows": ["....", "...."],
          "places": {
            "cortile": {"x": 0, "y": 0, "w": 4, "h": 2},
            "pozzo": {"x": 1, "y": 0, "w": 1, "h": 1}
          }
        }
        """);

        Assert.That(loaded.IsOk, Is.True);
        Assert.That(loaded.Value!.PlaceAt(new Cell(1, 0)), Is.EqualTo("cortile"));
    }

    [Test]
    public void UnaMappaCheNonCENonSiCaricaInSilenzio()
    {
        var loaded = VillageMap.Load(TestVillage.FixturePath("nessuna_mappa.json"));
        Assert.That(loaded.IsOk, Is.False);
        Assert.That(loaded.Code, Is.EqualTo("map_not_found"));
    }

    [Test]
    public void UnaMappaIlleggibileNonSiCaricaInSilenzio()
    {
        var loaded = VillageMap.FromJson("{\"width\": ");
        Assert.That(loaded.IsOk, Is.False);
        Assert.That(loaded.Code, Is.EqualTo("invalid_map"));
    }

    [Test]
    public void UnaVoceDiLegendaCheNonEUnCarattereNonPassa()
    {
        var loaded = VillageMap.FromJson("""
        {"width": 1, "height": 1, "legend": {"..": true}, "rows": ["."]}
        """);
        Assert.That(loaded.IsOk, Is.False);
        Assert.That(loaded.Code, Is.EqualTo("invalid_legend"));
    }
}
