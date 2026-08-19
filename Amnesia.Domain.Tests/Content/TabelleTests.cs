using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Amnesia.Core;
using Amnesia.Llm;
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
        ["diario"] = "rosa",
        ["scatola_vestiti"] = "rosa",
        ["cartella_clinica"] = "rosa",
        ["quaderno_vittorio"] = Magazzino,
        ["registro"] = Magazzino,
        ["cassetta_latta"] = Magazzino,
        ["braccialetto"] = Magazzino,
        ["busta_andrea"] = "don_carlo",
        ["due_righe_matteo"] = "matteo",
    };

    [Test]
    public void LeDueTabelleSiCaricanoESonoQuelleDelGiocoIntero()
    {
        Assert.That(Declarations().Ids.Count, Is.GreaterThanOrEqualTo(25),
            "meno di venticinque righe non e' un'indagine");
        Assert.That(Scale().Keys, Is.EquivalentTo(new[] { "matteo", "anna", "laura", "don_carlo" }),
            "hanno una scala i quattro guardinghi, e nessun altro");
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
        foreach (var npcId in new[] { "rosa", "nino", "wanda", "elena" })
        {
            Assert.That(positions.HasLadder(npcId), Is.False, $"{npcId} non e' guardingo e non ha una scala");
            var sue = table.Ids.Where(id => table.SourcesOf(id).Contains(npcId)).ToList();
            Assert.That(sue, Is.Not.Empty,
                $"{npcId} non ha una scala e non e' fonte di niente: attraverso lo strumento sarebbe muto");
        }
    }

    /// Il coro. Le parole sono fissate una volta sola, in `rules.md`, e la
    /// tabella deve dire quelle: una virgola diversa e la prova si annacqua,
    /// perche' la prova e' che la frase esca identica da bocche diverse.
    [Test]
    public void LaVersioneDelPaeseEUnCoroEEscePariPariDaOgniBocca()
    {
        var table = Declarations();
        var testo = table.TextOf("versione_paese");
        var regole = Regex.Replace(File.ReadAllText(PathOf("prompts", "rules.md")).Replace(">", " "), @"\s+", " ");

        Assert.That(regole, Does.Contain(testo),
            "il testo canonico del coro e' esattamente quello fissato nelle regole della recitazione");
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
        partita.Dice("rosa", "usciva_allegro");
        partita.Dice("nino", "avevano_una_frase");
        partita.Dice("nino", "dove_lo_trovai");
        partita.Dice("don_carlo", "busta_esiste");

        // Atto II. La frase apre i membri, e Anna dice dov'e' il magazzino.
        partita.Mostra("anna", "frase");
        Assert.That(partita.Posizione("anna"), Is.EqualTo("A1"));
        partita.Dice("anna", "magazzino_dove");
        partita.ApreIlMagazzino();

        // Il braccialetto apre il rito. Anna fa l'elenco di chi c'era e chi no.
        partita.Mostra("anna", "braccialetto");
        Assert.That(partita.Posizione("anna"), Is.EqualTo("A2"));
        partita.Dice("anna", "sacrificio_per_vittorio");
        partita.Dice("anna", "anna_solo_matteo");
        partita.Dice("anna", "padre_nella_cava");
        partita.Dice("anna", "io_ero_con_lui");

        // Atto III. Matteo: la frase, poi il braccialetto, poi il registro delle
        // presenze — e la versione che reggeva da ventun anni non regge piu'.
        partita.Mostra("matteo", "frase");
        partita.Dice("matteo", "matteo_ero_gia_sceso");
        partita.Dice("matteo", "non_erano_gite");
        partita.Mostra("matteo", "braccialetto");
        partita.Dice("matteo", "il_rito");
        Assert.That(partita.Posizione("matteo"), Is.EqualTo("M2"),
            "il braccialetto non basta: lui era la' sotto e sa benissimo che c'era una bambina");
        partita.Accosta("matteo", "anna_solo_matteo", "matteo_ero_gia_sceso");
        Assert.That(partita.Posizione("matteo"), Is.EqualTo("M3"),
            "cede a due frasi che sono in piazza da ventun anni e che nessuno aveva mai messo vicine");
        partita.Dice("matteo", "matteo_la_porto_via");
        partita.Dice("matteo", "elena_viva");
        partita.Dice("matteo", "matteo_non_dice_dove");

        // Don Carlo consegna la busta a chi e' venuto a chiedere di una persona viva.
        partita.Mostra("don_carlo", "braccialetto");
        partita.Dice("don_carlo", "don_carlo_manda");
        partita.Mostra("don_carlo", "foglio_indirizzo");
        partita.Dice("don_carlo", "don_carlo_consegna");
        Assert.That(partita.InTasca, Does.Contain("busta_andrea"), "la busta del padre arriva in mano");

        // Atto IV. Il foglio per Chivasso, il rifiuto, e la segatura nei risvolti.
        partita.Mostra("matteo", "foglio_indirizzo");
        Assert.That(partita.Posizione("matteo"), Is.EqualTo("M4"));
        partita.Dice("matteo", "matteo_mai_parlati");
        // Le cose che un oggetto dice le legge il giocatore quando ce l'ha in
        // mano: la segatura sta nei risvolti dei pantaloni, e a vederla e' lui.
        partita.Dice("scatola_vestiti", "segatura");
        partita.Accosta("matteo", "matteo_mai_parlati", "segatura");
        Assert.That(partita.Posizione("matteo"), Is.EqualTo("M5"));
        partita.Dice("matteo", "matteo_confessa");

        var ultimo = Scale()["matteo"].Last().Id;
        Assert.That(partita.Posizione("matteo"), Is.EqualTo(ultimo), "Matteo e' arrivato in cima alla sua scala");
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

        /// Accostare due righe che sono state dette. Il motore lo consente solo
        /// se entrambe sono nel registro — la stessa proprieta' di Mostra,
        /// applicata alle parole invece che agli oggetti.
        public void Accosta(string npcId, string first, string second)
        {
            var register = new Register(_world);
            foreach (var id in new[] { first, second })
            {
                Assert.That(register.SupportsFor(id), Is.Not.Empty,
                    $"il giocatore non puo' accostare una riga che non ha raccolto: {id}");
            }
            _world.MarkConfrontoShown(npcId, first, second);
            Raccogli();
        }

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

        /// Cio' che un personaggio consegna quando e' arrivato in cima.
        private void Raccogli()
        {
            if (Posizione("matteo") == "M5")
            {
                _inTasca.Add("due_righe_matteo");
            }
            if (Posizione("don_carlo") == "C2")
            {
                _inTasca.Add("busta_andrea");
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
    }
}
