using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Llm;
using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.Content;

/// Le due tabelle vere — quelle che il gioco carica, non le fixture.
///
/// Un contenuto che si carica non e' un contenuto che funziona: una riga che
/// nessuna bocca puo' dire, un gradino appeso a un oggetto che nessuno puo'
/// avere in mano, un personaggio senza scala che non e' fonte di niente non
/// rompono niente e non lo dice nessuno. Costano una partita che si ferma, e
/// tre giorni per capire dove.
public class TabelleTests
{
    /// Il contenuto sta nella radice del repository, non accanto alle dll: si
    /// risale finche' non si trova la cartella che contiene il paese.
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

    private static string PathOf(params string[] parts) =>
        Path.Combine(new[] { ContentDir() }.Concat(parts).ToArray());

    /// Un saluto appeso a un gradino che non esiste non rompe niente: quel
    /// personaggio semplicemente non ti saluta piu', e nessuno se ne accorge
    /// finche' non gioca proprio quel pezzo.
    [Test]
    public void OgniSalutoParlaDiUnGradinoCheEsiste()
    {
        var caricati = GreetingTable.Load(PathOf("amnesia", "saluti.json"));
        Assert.That(caricati.IsOk, Is.True, caricati.Message);
        var saluti = caricati.Value!;
        var scale = Positions();

        using var documento = JsonDocument.Parse(File.ReadAllText(PathOf("amnesia", "saluti.json")));
        foreach (var personaggio in documento.RootElement.GetProperty("greetings").EnumerateObject())
        {
            foreach (var riga in personaggio.Value.EnumerateObject())
            {
                Assert.That(riga.Value.GetString(), Is.Not.Empty, $"{personaggio.Name}/{riga.Name} e' vuoto");
                if (riga.Name.Length == 0)
                {
                    Assert.That(scale.HasLadder(personaggio.Name), Is.False,
                        $"{personaggio.Name} ha una scala: i suoi saluti vanno per gradino");
                    continue;
                }
                Assert.That(scale.HasLadder(personaggio.Name), Is.True, $"{personaggio.Name} non ha una scala");
                Assert.That(GradiniDi(personaggio.Name), Contains.Item(riga.Name),
                    $"{personaggio.Name} non ha il gradino {riga.Name}");
            }
        }

        // Chi ha una scala deve avere il saluto del gradino da cui si parte, o
        // la prima conversazione del gioco comincia in silenzio.
        foreach (var chi in saluti.Ids.Where(chi => scale.HasLadder(chi)))
        {
            Assert.That(saluti.Line(chi, GradiniDi(chi).First()), Is.Not.Empty, $"{chi} non saluta al primo gradino");
        }
    }

    private static IReadOnlyList<string> GradiniDi(string npcId)
    {
        using var documento = JsonDocument.Parse(File.ReadAllText(PathOf("amnesia", "positions.json")));
        return documento.RootElement.GetProperty("positions").GetProperty(npcId)
            .EnumerateArray().Select(gradino => gradino.GetProperty("id").GetString()!).ToList();
    }

    private static DeclarationTable Declarations()
    {
        var loaded = DeclarationTable.Load(PathOf("amnesia", "declarations.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    private static PositionTable Positions()
    {
        var loaded = PositionTable.Load(PathOf("amnesia", "positions.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    /// `PositionTable` risponde a domande su un mondo, e non elenca i gradini:
    /// per interrogare i dati come dati la tabella si rilegge qui.
    private static Dictionary<string, List<GradinoDto>> Scale()
    {
        var dto = JsonSerializer.Deserialize<ScaleDto>(
            File.ReadAllText(PathOf("amnesia", "positions.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.That(dto, Is.Not.Null, "le scale si deserializzano");
        return dto!.Positions;
    }

    /// Le persone del gioco sono quelle che hanno una scheda: una fonte che non
    /// ha una scheda e' una bocca che non esiste.
    private static IReadOnlyCollection<string> Persone() =>
        Directory.GetFiles(PathOf("prompts"), "*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != "rules")
            .Select(name => name!)
            .ToHashSet();

    private static IReadOnlyCollection<string> Oggetti()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(PathOf("items.json")));
        return document.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()!)
            .ToHashSet();
    }

    /// Da dove esce ogni oggetto che una precondizione nomina — sezione 4 della
    /// spec del contenuto. Un gradino appeso a una cosa che nessuno puo' avere
    /// in mano tronca un personaggio in silenzio: la provenienza sta scritta, e
    /// si verifica.
    private const string Risveglio = "risveglio";
    private const string Magazzino = "magazzino";

    private static readonly IReadOnlyDictionary<string, string> Provenienza = new Dictionary<string, string>
    {
        ["fotografia"] = Risveglio,
        ["frase"] = Risveglio,
        ["foglio_indirizzo"] = Risveglio,
        ["chiave_b17"] = Risveglio,
        ["taccuino"] = Risveglio,
        ["lapide"] = "paese",
        ["giacca"] = "nino",
        ["quaderno_vittorio"] = Magazzino,
        ["registro"] = Magazzino,
        ["cassetta_latta"] = Magazzino,
        ["braccialetto"] = Magazzino,
        ["due_righe_matteo"] = "matteo",
    };

    [Test]
    public void LeDueTabelleSiCaricanoESonoQuelleDelGiocoIntero()
    {
        Assert.That(Declarations().Ids.Count, Is.GreaterThanOrEqualTo(25),
            "meno di venticinque righe non e' un'indagine");
        Assert.That(Scale().Keys, Is.EquivalentTo(new[] { "matteo", "anna", "laura", "don_carlo", "nino" }),
            "hanno una scala i quattro guardinghi — e Nino, che tiene la giacca");
    }

    /// Orfani nella prima direzione: un gradino che concede un id che la tabella
    /// non ha e' una riga che non comparira' mai nel prompt di nessuno.
    [Test]
    public void NessunGradinoConcedeUnaRigaCheNonEsiste()
    {
        var table = Declarations();
        var orfani = new List<string>();
        foreach (var (npcId, ladder) in Scale())
        {
            foreach (var declarationId in ladder.SelectMany(step => step.Grants))
            {
                if (!table.Has(declarationId))
                {
                    orfani.Add($"{npcId} -> {declarationId}");
                }
            }
        }
        Assert.That(orfani, Is.Empty, "ogni riga concessa da un gradino sta nella tabella delle dichiarazioni");
    }

    /// Orfani nell'altra direzione, che e' quella che non si vede: una riga
    /// scritta con cura che nessuna scala concede e di cui nessuno e' fonte
    /// resta nel file per sempre e non la sente mai nessuno.
    [Test]
    public void NessunaRigaRestaSenzaUnaBocca()
    {
        var table = Declarations();
        var positions = Positions();
        var concesse = Scale().Values.SelectMany(ladder => ladder.SelectMany(step => step.Grants)).ToHashSet();

        var mute = new List<string>();
        foreach (var declarationId in table.Ids)
        {
            // O gliela concede un gradino, o e' fonte chi non ha una scala —
            // una persona che non nasconde niente, oppure un documento che la
            // dice a chi lo legge.
            var senzaScala = table.SourcesOf(declarationId).Any(source => !positions.HasLadder(source));
            if (!concesse.Contains(declarationId) && !senzaScala)
            {
                mute.Add(declarationId);
            }
        }
        Assert.That(mute, Is.Empty, "ogni dichiarazione ha almeno una bocca o un documento da cui puo' uscire");
    }

    [Test]
    public void OgniFonteEUnaPersonaConUnaSchedaOUnOggettoCheLaDice()
    {
        var table = Declarations();
        var persone = Persone();
        var oggetti = Oggetti();

        var sconosciute = new List<string>();
        foreach (var declarationId in table.Ids)
        {
            foreach (var source in table.SourcesOf(declarationId))
            {
                if (!persone.Contains(source) && !oggetti.Contains(source))
                {
                    sconosciute.Add($"{declarationId} <- {source}");
                }
            }
            Assert.That(table.SourcesOf(declarationId), Is.Not.Empty, $"{declarationId} viene da qualche parte");
            Assert.That(table.TextOf(declarationId), Is.Not.Empty, $"{declarationId} ha un testo canonico");
        }
        Assert.That(sconosciute, Is.Empty, "ogni fonte e' una persona con una scheda o un oggetto del catalogo");
    }

    [Test]
    public void OgniPrecondizioneNominaUnOggettoCheEsiste()
    {
        var table = Declarations();
        var oggetti = Oggetti();

        var inventati = new List<string>();
        foreach (var declarationId in table.Ids)
        {
            foreach (var itemId in table.RequiresShown(declarationId))
            {
                if (!oggetti.Contains(itemId))
                {
                    inventati.Add($"{declarationId} chiede {itemId}");
                }
            }
        }
        foreach (var (npcId, ladder) in Scale())
        {
            foreach (var itemId in ladder.SelectMany(step => step.RequiresShown))
            {
                if (!oggetti.Contains(itemId))
                {
                    inventati.Add($"{npcId} chiede {itemId}");
                }
            }
        }
        Assert.That(inventati, Is.Empty, "ogni precondizione nomina un oggetto che sta nel catalogo");
    }

    /// La scala concede, la colonna delle fonti dice da quale bocca puo' uscire:
    /// se divergono, il file racconta una cosa e il gioco ne fa un'altra.
    [Test]
    public void CioCheUnGradinoConcedeLoDiceAncheLaColonnaDelleFonti()
    {
        var table = Declarations();
        var incoerenti = new List<string>();
        foreach (var (npcId, ladder) in Scale())
        {
            foreach (var declarationId in ladder.SelectMany(step => step.Grants).Distinct())
            {
                if (!table.SourcesOf(declarationId).Contains(npcId))
                {
                    incoerenti.Add($"{npcId} puo' dire {declarationId} ma non ne e' fonte");
                }
            }
        }
        Assert.That(incoerenti, Is.Empty, "scala e fonti dicono la stessa cosa");
    }

    /// Un gradino appeso a una cosa che nessuno puo' avere in mano non e' un
    /// errore: e' un personaggio che si ferma dov'e' e non lo dice a nessuno.
    [Test]
    public void OgniGradinoSiPuoRaggiungere()
    {
        var irraggiungibili = new List<string>();
        var circolari = new List<string>();
        foreach (var (npcId, ladder) in Scale())
        {
            foreach (var step in ladder)
            {
                foreach (var itemId in step.RequiresShown)
                {
                    if (!Provenienza.TryGetValue(itemId, out var da))
                    {
                        irraggiungibili.Add($"{npcId} {step.Id} chiede {itemId}, che non viene da nessuna parte");
                        continue;
                    }
                    // Il foglio per Wanda lo scrive Matteo in cima alla sua
                    // scala, la busta la consegna Don Carlo in cima alla sua: un
                    // gradino appeso a cio' che quel gradino stesso produce e'
                    // una porta chiusa a chiave dall'interno.
                    if (da == npcId)
                    {
                        circolari.Add($"{npcId} {step.Id} chiede {itemId}, che pero' lo da' lui");
                    }
                }
            }
        }
        Assert.That(irraggiungibili, Is.Empty, "ogni gradino chiede una cosa che il giocatore puo' avere in mano");
        Assert.That(circolari, Is.Empty, "e nessun personaggio custodisce la chiave del proprio gradino");
    }

    [Test]
    public void IQuattroSenzaScalaHannoQualcosaDaDire()
    {
        var table = Declarations();
        var positions = Positions();
        foreach (var npcId in new[] { "rosa", "wanda", "elena", "teresa" })
        {
            Assert.That(positions.HasLadder(npcId), Is.False, $"{npcId} non e' guardingo e non ha una scala");
            var sue = table.Ids.Where(id => table.SourcesOf(id).Contains(npcId)).ToList();
            Assert.That(sue, Is.Not.Empty,
                $"{npcId} non ha una scala e non e' fonte di niente: attraverso lo strumento sarebbe muto");
        }
    }

    /// Il coro. Le parole sono fissate una volta sola, nella dichiarazione, e
    /// arrivano a ogni bocca dalla posizione: una virgola diversa e la prova si
    /// annacqua, perche' la prova e' che la frase esca identica da bocche
    /// diverse. Nelle regole il coro NON c'e' piu': era un'istruzione permanente
    /// che faceva recitare la copertura anche a chi era gia' andato oltre.
    [Test]
    public void LaVersioneDelPaeseEUnCoroEEscePariPariDaOgniBocca()
    {
        var table = Declarations();
        var testo = table.TextOf("versione_paese");

        Assert.That(testo, Is.EqualTo(
            "È stata una disgrazia. Erano andati su a vedere la cava, la montagna è venuta giù, e per quella creatura è stato un attimo."),
            "il testo canonico del coro e' fissato qui, e non si cambia per sbaglio");
        var regole = File.ReadAllText(PathOf("prompts", "rules.md"));
        Assert.That(regole, Does.Not.Contain("per quella creatura"),
            "il coro non sta nelle regole: e' la posizione di partenza, non un ordine permanente");
        Assert.That(table.RequiresShown("versione_paese"), Is.Empty, "il coro non ha precondizioni: si dice e basta");

        // E ogni scala lo concede sul primo gradino, altrimenti chi ha una scala
        // resta fuori dal coro proprio mentre il paese lo canta.
        foreach (var (npcId, ladder) in Scale())
        {
            Assert.That(ladder[0].Grants, Does.Contain("versione_paese"),
                $"{npcId} e' del paese e la frase del paese ce l'ha dal primo minuto");
        }

        // Tre bocche, da un mondo freddo, senza che niente sia stato mostrato.
        var world = new WorldState();
        var service = new DeclarationService(table, Positions());
        foreach (var npcId in new[] { "rosa", "anna", "matteo" })
        {
            Assert.That(service.Declare(world, npcId, "versione_paese").IsOk, Is.True, npcId);
        }
        Assert.That(new Register(world).SupportsFor("versione_paese").Count, Is.EqualTo(3),
            "tre bocche diverse, ed e' il giocatore ad accorgersene");
    }

    [Test]
    public void IlVocabolarioChiusoDelCatalogoCopreIlContenutoVero()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(ToolCatalog.Schemas()));
        var enumerati = document.RootElement.EnumerateArray()
            .Select(tool => tool.GetProperty("function"))
            .Single(function => function.GetProperty("name").GetString() == "dichiaro")
            .GetProperty("parameters").GetProperty("properties").GetProperty("id").GetProperty("enum")
            .EnumerateArray().Select(value => value.GetString()!).ToList();

        Assert.That(enumerati, Is.EquivalentTo(Declarations().Ids),
            "un id che il modello non puo' scegliere e' una riga che nessun personaggio dira' mai");
    }

    /// Le fixture dei test sono una copia dei file di contenuto, e devono
    /// restarlo: se divergono, la fetta si prova su dati che non sono quelli con
    /// cui si gioca, ed e' il modo piu' silenzioso di avere tutto verde e un
    /// gioco rotto.
    [Test]
    public void LeFixtureSonoLaCopiaEsattaDelContenuto()
    {
        foreach (var fileName in new[] { "declarations.json", "positions.json" })
        {
            var fixture = Path.Combine(TestContext.CurrentContext.TestDirectory, "Amnesia", "fixtures", fileName);
            Assert.That(File.ReadAllText(fixture), Is.EqualTo(File.ReadAllText(PathOf("amnesia", fileName))),
                $"{fileName}: la fixture e' andata alla deriva dal contenuto");
        }
    }

    /// **Il test che vale tutti gli altri.** Da un mondo freddo esiste una
    /// sequenza di cose mostrate e di cose dette che arriva in cima alla scala
    /// di Matteo e apre la porta di Chivasso. Se questa non passa, il gioco non
    /// si puo' finire — e nessuna prova di formato se ne accorge.
    [Test]
    public void DaUnMondoFreddoLaCatenaFinoAllUltimoGradinoDiMatteoSiPercorre()
    {
        var partita = new Partita(Declarations(), Positions());

        // Atto I. La fotografia apre il paese: ogni faccia riconosciuta e' un nome.
        partita.Mostra("rosa", "fotografia");
        partita.Dice("rosa", "circolo_esisteva");
        partita.Dice("rosa", "padre_ricerche");
        partita.Dice("nino", "avevano_una_frase");
        partita.Dice("nino", "dove_lo_trovai");

        // Atto II. La frase apre i membri, e Anna dice dov'e' il magazzino.
        partita.Mostra("anna", "frase");
        Assert.That(partita.Posizione("anna"), Is.EqualTo("A1"));
        partita.Dice("anna", "magazzino_dove");
        partita.ApreIlMagazzino();

        // La prova della cava apre il rito. Anna fa l'elenco di chi c'era e chi no.
        partita.Mostra("anna", "braccialetto");
        Assert.That(partita.Posizione("anna"), Is.EqualTo("A2"));
        partita.Dice("anna", "sacrificio_per_vittorio");
        partita.Dice("anna", "anna_solo_matteo");
        partita.Dice("anna", "padre_nella_cava");
        partita.Dice("anna", "io_ero_con_lui");

        // Atto III. Matteo: la frase, poi UN oggetto del magazzino sul banco —
        // basta quello, e la scampagnata non regge piu': rito e ratto insieme.
        partita.Mostra("matteo", "frase");
        partita.Dice("matteo", "matteo_ero_gia_sceso");
        partita.Dice("matteo", "non_erano_gite");
        partita.Mostra("matteo", "quaderno_vittorio");
        Assert.That(partita.Posizione("matteo"), Is.EqualTo("M2"));
        partita.Dice("matteo", "il_rito");
        partita.Dice("matteo", "matteo_la_porto_via");
        partita.Dice("matteo", "elena_viva");
        partita.Dice("matteo", "matteo_non_dice_dove");

        // Atto IV. Col ratto confessato, il paese sa che Nino ha una cosa da
        // dare: la giacca ritrovata accanto al corpo.
        Assert.That(partita.InTasca, Does.Contain("giacca"), "col ratto confessato Nino consegna la giacca");
        // La si fa riconoscere — basta una bocca — e la si porta in faccia a Matteo.
        partita.Mostra("rosa", "giacca");
        partita.Dice("rosa", "giacca_e_di_matteo");
        partita.Mostra("matteo", "giacca");

        var ultimo = Scale()["matteo"].Last().Id;
        Assert.That(partita.Posizione("matteo"), Is.EqualTo(ultimo),
            "la giacca sua, mostrata in faccia, chiude la scala di Matteo");
        partita.Dice("matteo", "matteo_confessa");
        Assert.That(partita.InTasca, Does.Contain("due_righe_matteo"), "e concede il foglio per Wanda");

        // E la porta di Chivasso si apre.
        partita.Mostra("wanda", "due_righe_matteo");
        partita.Dice("wanda", "wanda_una_persona_sola");
        partita.Mostra("elena", "braccialetto");
        partita.Dice("elena", "elena_adottata");
    }

    /// Una partita giocata coi soli mezzi del gioco: si mostra solo cio' che si
    /// ha in mano, e si dice solo cio' che il motore concede.
    private sealed class Partita
    {
        private readonly WorldState _world = new();
        private readonly PositionTable _positions;
        private readonly DeclarationService _service;
        private readonly HashSet<string> _inTasca;

        public Partita(DeclarationTable declarations, PositionTable positions)
        {
            _positions = positions;
            _service = new DeclarationService(declarations, positions);
            _inTasca = Provenienza
                .Where(item => item.Value is Risveglio or "paese" or "rosa")
                .Select(item => item.Key)
                .ToHashSet();
        }

        public IReadOnlyCollection<string> InTasca => _inTasca;

        public string Posizione(string npcId) => _positions.PositionOf(npcId, _world);

        public void Mostra(string npcId, string itemId)
        {
            Assert.That(_inTasca, Does.Contain(itemId),
                $"il giocatore non puo' mostrare a {npcId} una cosa che non ha: {itemId}");
            _world.MarkShown(npcId, itemId);
            Raccogli();
        }

        public void Dice(string npcId, string declarationId)
        {
            var said = _service.Declare(_world, npcId, declarationId);
            Assert.That(said.IsOk, Is.True, $"{npcId} non puo' dire {declarationId}: {said.Message}");
            Raccogli();
        }

        /// La chiave d'ottone ce l'ha dal risveglio: gli mancava dov'e'.
        public void ApreIlMagazzino()
        {
            Assert.That(new Register(_world).SupportsFor("magazzino_dove"), Is.Not.Empty,
                "nessuno gli ha ancora detto dov'e' il diciassette");
            foreach (var item in Provenienza.Where(item => item.Value == Magazzino))
            {
                _inTasca.Add(item.Key);
            }
        }

        /// Cio' che i gradini raggiunti hanno da consegnare: la stessa riga di
        /// dati che usa il motore, cosi' il banco di prova non puo' mentire.
        private void Raccogli()
        {
            foreach (var npcId in new[] { "matteo", "don_carlo", "nino" })
            {
                foreach (var itemId in _positions.ConsegnateFinora(npcId, _world))
                {
                    _inTasca.Add(itemId);
                }
            }
        }
    }

    private sealed class ScaleDto
    {
        [JsonPropertyName("positions")]
        public Dictionary<string, List<GradinoDto>> Positions { get; set; } = new();
    }

    private sealed class GradinoDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("grants")]
        public List<string> Grants { get; set; } = new();

        [JsonPropertyName("requires_shown")]
        public List<string> RequiresShown { get; set; } = new();

        [JsonPropertyName("requires_any_shown")]
        public List<string> RequiresAnyShown { get; set; } = new();
    }

    /// Una porta con le coordinate sbagliate non rompe niente: il gioco parte,
    /// il battente si costruisce da qualche parte fuori dal paese, e quella
    /// stanza non si apre piu'. Lo si scopre camminando, e non si capisce
    /// perche'.
    [Test]
    public void OgniPortaSivaAdApriteConLeProprieGambe()
    {
        var luoghi = PlaceTable.Load(PathOf("amnesia", "luoghi.json"));
        Assert.That(luoghi.IsOk, Is.True, luoghi.Message);
        var mappa = VillageMap.Load(PathOf("village_map.json")).Value!;
        var catalogo = Items();

        foreach (var id in luoghi.Value!.Ids)
        {
            var luogo = luoghi.Value.Find(id)!;
            var cella = luogo.Door.Cell();
            Assert.That(cella.X, Is.InRange(0, mappa.Width - 1), $"la porta di {id} e' fuori dalla mappa");
            Assert.That(cella.Y, Is.InRange(0, mappa.Height - 1), $"la porta di {id} e' fuori dalla mappa");

            // Ci si deve poter arrivare davanti: una serranda dentro un muro non
            // la apre nessuno.
            var accostabile = mappa.IsWalkable(cella)
                || mappa.IsWalkable(new Cell(cella.X + 1, cella.Y))
                || mappa.IsWalkable(new Cell(cella.X - 1, cella.Y))
                || mappa.IsWalkable(new Cell(cella.X, cella.Y + 1))
                || mappa.IsWalkable(new Cell(cella.X, cella.Y - 1));
            Assert.That(accostabile, Is.True, $"davanti alla porta di {id} non ci si arriva");

            foreach (var oggetto in luogo.Contains)
            {
                Assert.That(catalogo.Find(oggetto), Is.Not.Null, $"{id} contiene {oggetto}, che non sta nel catalogo");
            }
            if (luogo.RequiresItem.Length > 0)
            {
                Assert.That(catalogo.Find(luogo.RequiresItem), Is.Not.Null,
                    $"{id} chiede {luogo.RequiresItem}, che non sta nel catalogo");
            }
        }
    }

    /// Se un oggetto fa salire un personaggio di gradino, la reazione a
    /// quell'oggetto DEVE essere una battuta d'autore — del gradino nuovo, di uno
    /// gia' raggiunto o del suo «sempre» — mai il ripiego generico «questa cosa
    /// non la conosci». Anna che sale in A2 col quaderno e nella stessa riga dice
    /// di non riconoscerlo e' il gioco che si contraddice.
    ///
    /// L'eccezione onesta e' Matteo alla frase: sale in M1 ma resta sul panico di
    /// M0, che e' comunque una reazione scritta e non il default. Percio' la
    /// regola non e' «la reazione sta nel gradino nuovo», ma «la reazione di un
    /// oggetto che fa salire non e' mai il ripiego generico».
    [Test]
    public void OgniOggettoCheFaSalireHaLaSuaReazioneENonIlRipiego()
    {
        var reazioni = ReactionTable.Load(PathOf("amnesia", "reazioni.json"));
        Assert.That(reazioni.IsOk, Is.True, reazioni.Message);
        var tavola = reazioni.Value!;

        // I default generici, per confronto: la reazione risolta non deve mai
        // coincidere con uno di questi quando l'oggetto e' cio' che fa salire.
        var generici = new Dictionary<string, string>();
        using (var doc = JsonDocument.Parse(File.ReadAllText(PathOf("amnesia", "reazioni.json"))))
        {
            var comune = doc.RootElement.GetProperty("Reazioni").GetProperty("chiunque").GetProperty("sempre");
            foreach (var voce in comune.EnumerateObject())
            {
                generici[voce.Name] = voce.Value.GetString() ?? "";
            }
        }

        foreach (var (npc, scala) in Scale())
        {
            // I gradini raggiunti fin qui, in ordine: la reazione si risolve
            // esattamente su questi, come nel turno in cui l'oggetto sale sul banco.
            var idFinora = new List<string>();
            foreach (var gradino in scala)
            {
                idFinora.Add(gradino.Id);
                foreach (var oggetto in gradino.RequiresShown.Concat(gradino.RequiresAnyShown).Distinct())
                {
                    var reazione = tavola.Reazione(npc, idFinora, oggetto);
                    Assert.That(reazione, Is.Not.Empty,
                        $"{npc}/{gradino.Id}: «{oggetto}» fa salire ma non ha reazione — cade sul default muto");
                    if (generici.TryGetValue(oggetto, out var difetto))
                    {
                        Assert.That(reazione, Is.Not.EqualTo(difetto),
                            $"{npc}/{gradino.Id}: «{oggetto}» fa salire ma la reazione e' il ripiego generico — contraddice il gradino");
                    }
                }
            }
        }
    }

    private static ItemCatalog Items()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var definizioni = JsonSerializer.Deserialize<List<ItemDefinition>>(File.ReadAllText(PathOf("items.json")), options);
        Assert.That(definizioni, Is.Not.Null);
        return new ItemCatalog(definizioni!);
    }
}
