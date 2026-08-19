using System.Text.Json;
using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.Content;

/// I tre file di contenuto veri, quelli che il gioco carica, non le fixture.
///
/// Un file di contenuto che si carica ma e' impercorribile passa ogni prova di
/// formato e poi costa una giornata: il personaggio parte, cammina finche' il
/// navigatore non trova piu' strada, e resta li' senza che nessuno sollevi un
/// errore. Percio' qui la raggiungibilita' a piedi si dimostra, cella per cella,
/// invece di darla per scontata.
public class ContenutoTests
{
    private const string Casa = "casa_lipari";

    /// Il paese e' contenuto, e il contenuto sta nella radice del repository,
    /// non accanto alle dll. Si risale finche' non si trova la cartella che li
    /// contiene tutti e tre: cosi' la stessa prova vale da riga di comando e da
    /// dentro un editor, che partono da directory diverse.
    private static string ContentDir()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content");
            if (File.Exists(Path.Combine(candidate, "village_map.json")))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        Assert.Fail("la cartella content non si trova risalendo da " + TestContext.CurrentContext.TestDirectory);
        return "";
    }

    private static string PathOf(string fileName) => Path.Combine(ContentDir(), fileName);

    private static VillageMap Map()
    {
        var loaded = VillageMap.Load(PathOf("village_map.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    private static RoutineTable Routines()
    {
        var loaded = RoutineTable.Load(PathOf("routines.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    /// ItemCatalog prende una sequenza di ItemDefinition, e il file e' scritto
    /// esattamente in quella forma: finche' il dominio non ha un caricatore suo,
    /// la lettura sta qui e resta una riga sola.
    private static ItemCatalog Items()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var definitions = JsonSerializer.Deserialize<List<ItemDefinition>>(File.ReadAllText(PathOf("items.json")), options);
        Assert.That(definitions, Is.Not.Null, "il catalogo degli oggetti si deserializza");
        return new ItemCatalog(definitions!);
    }

    [Test]
    public void ITreFileDiContenutoSiCaricano()
    {
        var map = Map();
        Assert.That(map.Width, Is.EqualTo(48));
        Assert.That(map.Height, Is.EqualTo(32));
        Assert.That(Routines().Entries, Is.Not.Empty);
        Assert.That(Items().Items, Is.Not.Empty);
    }

    /// Le due regressioni che questo formato invita: una riga ritoccata a mano
    /// diventa una striscia invisibile e non calpestabile in fondo alla mappa, e
    /// un carattere fuori legenda diventa un buco nel disegno del paese.
    [Test]
    public void OgniRigaELungaWidthEOgniCarattereStaInLegenda()
    {
        var map = Map();
        Assert.That(map.Rows.Count, Is.EqualTo(map.Height), "il file ha esattamente height righe");

        var ragged = new List<int>();
        var strays = new SortedSet<char>();
        for (var y = 0; y < map.Rows.Count; y++)
        {
            if (map.Rows[y].Length != map.Width)
            {
                ragged.Add(y);
            }
            foreach (var drawn in map.Rows[y])
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
    public void OgniLuogoDichiaratoSiRaggiungeAPiediDaCasaLipari()
    {
        var map = Map();
        var nav = new Navigator(map);
        var start = map.CenterOf(Casa);
        Assert.That(start, Is.Not.Null, "casa Lipari esiste, e il giocatore ci si sveglia");

        var muri = new List<string>();
        var isole = new List<string>();
        foreach (var place in map.Places.Keys)
        {
            var cells = map.CellsOf(place);
            Assert.That(cells, Is.Not.Empty, $"il luogo {place} ha delle celle");
            foreach (var cell in cells)
            {
                if (!map.IsWalkable(cell))
                {
                    muri.Add($"{place} {cell}");
                }
            }
            // Il centro, perche' e' li' che il sistema di movimento manda chi ha
            // quel luogo in routine: un luogo il cui centro non si raggiunge e'
            // un incarico che non finisce mai.
            var target = map.CenterOf(place)!.Value;
            if (!nav.Reachable(start.Value, target))
            {
                isole.Add(place);
            }
        }
        Assert.That(muri, Is.Empty, "nessun luogo dichiarato cade dentro un muro");
        Assert.That(isole, Is.Empty, "ogni luogo si raggiunge a piedi da casa Lipari");
    }

    [Test]
    public void OgniAttoreNasceDentroLaMappaEDentroUnLuogo()
    {
        var map = Map();
        var fuori = new List<string>();
        foreach (var spawn in map.Spawns)
        {
            if (!map.IsWalkable(spawn.Value) || map.PlaceAt(spawn.Value).Length == 0)
            {
                fuori.Add(spawn.Key);
            }
        }
        Assert.That(fuori, Is.Empty, "ogni attore nasce su una cella calpestabile dentro un luogo con un nome");
        Assert.That(map.Spawn(MovementSystem.Player), Is.Not.Null, "il giocatore ha un posto dove svegliarsi");
        Assert.That(map.PlaceAt(map.Spawn(MovementSystem.Player)!.Value), Is.EqualTo(Casa));
    }

    [Test]
    public void NessunaRoutineNominaUnLuogoCheLaMappaNonHa()
    {
        var map = Map();
        var routines = Routines();
        var sconosciuti = new List<string>();
        var senzaCorpo = new List<string>();
        var fuoriOrdine = new List<string>();
        foreach (var schedule in routines.Entries)
        {
            // Una routine per chi non ha una posizione di partenza non muove
            // nessuno e non lo segnala: e' un refuso che si legge come una scelta
            // di regia.
            if (map.Spawn(schedule.Key) is null)
            {
                senzaCorpo.Add(schedule.Key);
            }
            var previous = int.MinValue;
            foreach (var entry in schedule.Value)
            {
                if (!map.HasPlace(entry.Place))
                {
                    sconosciuti.Add($"{schedule.Key} -> {entry.Place}");
                }
                // PlaceFor tiene l'ultima voce gia' scattata nell'ordine del
                // file: una voce fuori ordine non e' un errore di formato, e'
                // un personaggio che alle nove sta dove doveva stare alle sette.
                if (entry.FromMinute < previous)
                {
                    fuoriOrdine.Add($"{schedule.Key} {entry.FromMinute}");
                }
                previous = entry.FromMinute;
            }
        }
        Assert.That(sconosciuti, Is.Empty, "ogni luogo nominato da una routine esiste sulla mappa");
        Assert.That(senzaCorpo, Is.Empty, "ogni personaggio con una routine ha anche una posizione di partenza");
        Assert.That(fuoriOrdine, Is.Empty, "le voci di ogni routine sono in ordine di minuto crescente");
    }

    [Test]
    public void NessunOggettoHaLaFacciaVisibileVuota()
    {
        var catalog = Items();
        var vuoti = new List<string>();
        var ids = new HashSet<string>();
        foreach (var item in catalog.Items)
        {
            Assert.That(item.Id, Is.Not.Empty, "ogni oggetto ha un id");
            Assert.That(ids.Add(item.Id), Is.True, $"l'id {item.Id} compare una volta sola");
            if (item.Visible.Trim().Length == 0)
            {
                vuoti.Add(item.Id);
            }
            Assert.That(item.Name, Is.Not.Empty, $"{item.Id} ha un'etichetta per l'inventario");
            Assert.That(item.Description, Is.Not.Empty, $"{item.Id} ha una descrizione per il giocatore");
        }
        Assert.That(vuoti, Is.Empty, "nessun oggetto arriva nel prompt come faccia vuota");
        // VisibleOf ripiega sull'id quando la faccia manca: se ripiega, quello che
        // il personaggio vede sul tavolo e' una sigla di database.
        foreach (var item in catalog.Items)
        {
            Assert.That(catalog.VisibleOf(item.Id), Is.Not.EqualTo(item.Id), $"{item.Id} non ripiega sul proprio id");
        }
    }

    [Test]
    public void GliOggettiDellaPrimaScenaCiSono()
    {
        var catalog = Items();
        foreach (var id in new[] { "fotografia", "foglio_indirizzo", "chiave_b17", "taccuino", "frase" })
        {
            Assert.That(catalog.Find(id), Is.Not.Null, $"{id} sta nel catalogo dal risveglio");
        }
    }

    /// Il pezzo di Nino regge solo se la geografia lo porta: il castagneto dove
    /// e' stato trovato Giorgio sta sopra la curva, e quella curva e' sotto la
    /// bottega di Matteo. Se la mappa non lo dice, quella battuta non significa
    /// niente e un atto intero non funziona.
    [Test]
    public void IlCastagnetoStaSopraLaBottegaELaCurvaSubitoSotto()
    {
        var map = Map();
        var bottega = map.Places["bottega"];
        var castagneto = map.Places["castagneto"];

        Assert.That(castagneto.Y + castagneto.H, Is.LessThanOrEqualTo(bottega.Y), "il castagneto sta piu' in alto della bottega");
        Assert.That(bottega.Y - (castagneto.Y + castagneto.H), Is.LessThanOrEqualTo(4), "e le sta addosso, non dall'altra parte del paese");
        Assert.That(castagneto.X, Is.GreaterThanOrEqualTo(bottega.X - 2), "e sulla stessa verticale");
        Assert.That(castagneto.X + castagneto.W, Is.LessThanOrEqualTo(bottega.X + bottega.W + 2));

        // La curva: una cella di strada sotto la bottega e nelle sue colonne, che
        // ha strada sia in verticale sia in orizzontale — cioe' dove la strada
        // gira. E' il punto da cui si passa per andare da Matteo.
        var curva = new List<Cell>();
        for (var y = bottega.Y + bottega.H; y < Math.Min(bottega.Y + bottega.H + 4, map.Height); y++)
        {
            for (var x = bottega.X; x < bottega.X + bottega.W; x++)
            {
                var cell = new Cell(x, y);
                if (map.Terrain(cell) != ',')
                {
                    continue;
                }
                var verticale = map.Terrain(new Cell(x, y - 1)) == ',' || map.Terrain(new Cell(x, y + 1)) == ',';
                var orizzontale = map.Terrain(new Cell(x - 1, y)) == ',' || map.Terrain(new Cell(x + 1, y)) == ',';
                if (verticale && orizzontale)
                {
                    curva.Add(cell);
                }
            }
        }
        Assert.That(curva, Is.Not.Empty, "la strada fa una curva subito sotto la bottega");
    }

    /// I minuti sono la valuta del gioco, e la mappa e' dove si stampano: a
    /// sedici celle al minuto, il paese si attraversa in tre minuti e alla cava
    /// si sale. Se un giorno il paese si rimpicciolisce, questo si accorge che
    /// andare a Pian della Soglia non costa piu' niente.
    [Test]
    public void AttraversareIlPaeseCostaTreMinutiESalireAllaCavaMoltiDiPiu()
    {
        var map = Map();
        var nav = new Navigator(map);
        var casa = map.CenterOf(Casa)!.Value;

        var paese = nav.Path(casa, map.CenterOf("casa_valli")!.Value).Count;
        var cava = nav.Path(casa, map.CenterOf("cava")!.Value).Count;

        var minutiPaese = (double)paese / MovementSystem.WalkCellsPerMinute;
        var minutiCava = (double)cava / MovementSystem.WalkCellsPerMinute;

        Assert.That(minutiPaese, Is.InRange(2.0, 4.0), $"attraversare il paese costa circa tre minuti (e' {minutiPaese:0.0})");
        Assert.That(minutiCava, Is.GreaterThan(minutiPaese * 1.5), $"la cava e' un'altra cosa (e' {minutiCava:0.0} contro {minutiPaese:0.0})");
    }
}
