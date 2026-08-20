using System.Collections.Generic;
using System.IO;
using Amnesia;
using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Game;
using Amnesia.Llm;
using Amnesia.World;
using UnityEngine;

namespace AmnesiaUnity
{
    /// Costruisce a runtime tutto quello che serve a provare il gioco: niente e'
    /// salvato in una scena, e questo e' voluto. Finche' il look non e' deciso,
    /// una scena e' una cosa che si rompe in silenzio quando cambia un prefab;
    /// venti righe di codice no.
    public sealed class Bootstrap : MonoBehaviour
    {
        [Tooltip("Il paese e' una griglia di celle. Questa e' quanto vale una cella in metri.")]
        public float CellSize = 1.0f;

        [Tooltip("Risoluzione verticale di rendering. Bassa apposta: e' la direzione artistica, non un ripiego.")]
        public int RenderHeight = 180;

        public WorldState World { get; private set; }
        public VillageMap Map { get; private set; }
        public ItemCatalog Items { get; private set; }
        public ConversationSession Session { get; private set; }
        public Greeter Accoglienza { get; private set; }
        public PlaceService Porte { get; private set; }
        public string Problema { get; private set; } = "";

        private readonly Dictionary<string, Transform> _corpi = new Dictionary<string, Transform>();
        private readonly Dictionary<string, string> _nomi = new Dictionary<string, string>();
        private DeclarationTable _dichiarazioni;
        private ConvinzioniTable _convinzioni = ConvinzioniTable.Empty();
        private PlaceTable _luoghi;
        private readonly Dictionary<string, Transform> _porte = new Dictionary<string, Transform>();

        /// Il taccuino si costruisce sul mondo di adesso e non si conserva: la
        /// sessione sostituisce il mondo a ogni turno riuscito, e un taccuino
        /// tenuto da parte leggerebbe la partita di due battute fa.
        public Taccuino Taccuino => new Taccuino(Session.World, _convinzioni, Items);

        /// Dove sta il contenuto. Nell'editor si legge direttamente la cartella
        /// `content/` del repository, cosi' una modifica alla mappa o a una
        /// scheda si vede premendo Play — senza ricopiare niente. Nella build
        /// quella cartella non esiste e si usa la copia in StreamingAssets.
        private static string Contenuto(params string[] parti)
        {
            var relativo = Path.Combine(parti);
            if (Application.isEditor)
            {
                var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content"));
                var candidato = Path.Combine(repository, relativo);
                if (File.Exists(candidato) || Directory.Exists(candidato))
                {
                    return candidato;
                }
            }
            return Path.Combine(Application.streamingAssetsPath, relativo);
        }

        private void Awake()
        {
            if (!Carica())
            {
                enabled = false;
                return;
            }
            CostruisciIlPaese();
            CostruisciLeFigure();
            CostruisciLePorte();
        }

        private bool Carica()
        {
            var mappa = VillageMap.Load(Contenuto("village_map.json"));
            if (!mappa.IsOk)
            {
                Problema = $"mappa: {mappa.Message}";
                return false;
            }
            Map = mappa.Value;

            var items = JsonUtilityItems(Contenuto("items.json"));
            if (items == null)
            {
                Problema = "items.json illeggibile";
                return false;
            }
            Items = items;

            var dichiarazioni = DeclarationTable.Load(Contenuto("amnesia", "declarations.json"));
            var posizioni = PositionTable.Load(Contenuto("amnesia", "positions.json"));
            if (!dichiarazioni.IsOk || !posizioni.IsOk)
            {
                Problema = dichiarazioni.IsOk ? posizioni.Message : dichiarazioni.Message;
                return false;
            }

            World = new WorldState();
            // Ne' la frase ne' il taccuino stanno qui dentro. La frase non e' un
            // oggetto: e' una riga scritta, e si usa dicendola. Il taccuino non
            // e' roba che si mostra: e' la mano di Giorgio — quello che ci ha
            // scritto lo si cita, non lo si mette sul banco.
            foreach (var starting in new[] { "fotografia", "foglio_indirizzo", "chiave_b17" })
            {
                World.ItemOwners[starting] = "player";
            }
            PosizionaGliAttori();

            var schede = new Dictionary<string, string>();
            foreach (var file in Directory.GetFiles(Contenuto("prompts"), "*.md"))
            {
                var id = Path.GetFileNameWithoutExtension(file);
                if (id != "rules")
                {
                    schede[id] = File.ReadAllText(file);
                    _nomi[id] = TitoloDi(schede[id], id);
                }
            }
            var regole = File.ReadAllText(Contenuto("prompts", "rules.md"));

            // SICUREZZA: la chiave sta in un .env fuori dal progetto Unity, non
            // viene mai stampata e non entra in nessuna build. Se manca, lo si
            // dice subito e a voce alta invece di scoprirlo al primo turno.
            var chiave = EnvFile.LoadOpenRouterKey(Path.Combine(Application.dataPath, "..", "..", ".env"));
            if (!chiave.IsOk)
            {
                Problema = "manca la chiave OpenRouter: metti OPENROUTER_KEY in un file .env nella radice del progetto";
                return false;
            }

            var luoghi = PlaceTable.Load(Contenuto("amnesia", "luoghi.json"));
            if (!luoghi.IsOk)
            {
                Problema = luoghi.Message;
                return false;
            }
            Porte = new PlaceService(luoghi.Value);
            _luoghi = luoghi.Value;

            var saluti = GreetingTable.Load(Contenuto("amnesia", "saluti.json"));
            if (!saluti.IsOk)
            {
                Problema = saluti.Message;
                return false;
            }
            _dichiarazioni = dichiarazioni.Value;
            // Il taccuino nuovo: la tabella delle convinzioni. Senza il file il
            // taccuino resta bianco, e la partita parte lo stesso.
            var convinzioni = ConvinzioniTable.Load(Contenuto("amnesia", "taccuino.json"));
            if (convinzioni.IsOk)
            {
                _convinzioni = convinzioni.Value;
            }
            Accoglienza = new Greeter(saluti.Value, posizioni.Value);

            // Il copione delle reazioni e' un file a parte e puo' mancare in una
            // partita vecchia: senza, il motore usa il ripiego («non lo conosci,
            // dillo») e il gioco parte lo stesso.
            var reazioni = ReactionTable.Load(Contenuto("amnesia", "reazioni.json"));

            var tabelle = new DeclarationService(dichiarazioni.Value, posizioni.Value);
            var contesto = new ContextBuilder(regole, schede, Items, true,
                dichiarazioni.Value, posizioni.Value,
                reazioni.IsOk ? reazioni.Value : ReactionTable.Empty());
            Session = new ConversationSession(
                World, new ConversationLog(), contesto,
                new OpenRouterTransport(chiave.Value, 60), tabelle, Items, "deepseek/deepseek-v3.2");
            return true;
        }

        private static ItemCatalog JsonUtilityItems(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            // JsonUtility non legge un array in cima, quindi lo si incarta.
            var wrapped = "{\"items\":" + File.ReadAllText(path) + "}";
            var parsed = JsonUtility.FromJson<ItemsDto>(wrapped);
            if (parsed?.items == null)
            {
                return null;
            }
            var definizioni = new List<ItemDefinition>();
            foreach (var riga in parsed.items)
            {
                definizioni.Add(new ItemDefinition(riga.id, riga.visible, riga.name, riga.description));
            }
            return new ItemCatalog(definizioni);
        }

        [System.Serializable] private sealed class ItemsDto { public ItemDto[] items; }
        [System.Serializable] private sealed class ItemDto
        {
            public string id; public string name; public string visible; public string description;
        }

        private void PosizionaGliAttori()
        {
            foreach (var pair in new[]
            {
                ("player", "casa_lipari"), ("rosa", "casa_lipari"), ("matteo", "bottega"),
                ("anna", "casa_ferro"), ("laura", "casa_valli"), ("don_carlo", "canonica"),
                ("nino", "castagneto"), ("teresa", "giardino"), ("piero", "bar"),
                ("marisa", "negozio"), ("beppe", "panetteria"), ("lidia", "bar"),
                ("gino", "piazza"),
            })
            {
                // Il posto scritto nella mappa vince sul centro geometrico del
                // luogo: uno sta sulla soglia o dietro il banco, non nel mezzo
                // matematico della stanza.
                var dove = Map.Spawn(pair.Item1) ?? Map.CenterOf(pair.Item2);
                if (dove.HasValue)
                {
                    World.ActorOf(pair.Item1).Position = dove.Value;
                }
            }
        }

        /// Il nome per esteso non sta in una tabella a parte: e' il titolo della
        /// scheda, cioe' la stessa riga che il modello si ritrova nel prompt. Due
        /// posti in cui scrivere lo stesso nome sono due posti in cui divergera'.
        public string NomeDi(string id) => _nomi.TryGetValue(id, out var nome) ? nome : id;

        private static string TitoloDi(string scheda, string ripiego)
        {
            foreach (var riga in scheda.Split('\n'))
            {
                var pulita = riga.Trim();
                if (pulita.StartsWith("# "))
                {
                    return pulita.Substring(2).Trim();
                }
            }
            return ripiego;
        }

        public Vector3 InScena(Cell cell) => new Vector3(cell.X * CellSize, 0f, -cell.Y * CellSize);

        public Cell CellaDi(Vector3 punto) => new Cell(
            Mathf.Clamp(Mathf.RoundToInt(punto.x / CellSize), 0, Map.Width - 1),
            Mathf.Clamp(Mathf.RoundToInt(-punto.z / CellSize), 0, Map.Height - 1));

        private void CostruisciIlPaese()
        {
            Scenografia.Costruisci(Map, CellSize, new GameObject("paese").transform);
        }

        /// Il ferro sulla porta. Sta in scena solo finche' e' chiuso: quando si
        /// apre sparisce, e da quel momento quella stanza e' parte del paese
        /// come tutte le altre.
        private void CostruisciLePorte()
        {
            var porte = new GameObject("porte").transform;
            foreach (var id in _luoghi.Ids)
            {
                var luogo = _luoghi.Find(id);
                if (luogo == null || Porte.IsOpen(World, id))
                {
                    continue;
                }
                var battente = GameObject.CreatePrimitive(PrimitiveType.Cube);
                battente.name = id;
                battente.transform.SetParent(porte);
                if (luogo.Botola)
                {
                    // Per terra, e non in piedi: una botola disegnata come una
                    // porta si legge come un muro, e ci si gira intorno.
                    battente.transform.position = InScena(luogo.Door.Cell()) + Vector3.up * (CellSize * 0.06f);
                    battente.transform.localScale = new Vector3(CellSize * 0.9f, CellSize * 0.12f, CellSize * 0.9f);
                    battente.GetComponent<Renderer>().sharedMaterial =
                        Scenografia.Materiale(new Color(0.20f, 0.17f, 0.14f));
                }
                else
                {
                    battente.transform.position = InScena(luogo.Door.Cell()) + Vector3.up * (CellSize * 1.1f);
                    battente.transform.localScale = new Vector3(CellSize, CellSize * 2.2f, CellSize * 0.25f);
                    battente.GetComponent<Renderer>().sharedMaterial =
                        Scenografia.Materiale(new Color(0.24f, 0.21f, 0.18f));
                }
                _porte[id] = battente.transform;
            }
        }

        /// La porta chiusa piu' vicina, se ci sei davanti.
        public string PortaVicina(Vector3 da, float portata)
        {
            foreach (var pair in _porte)
            {
                if (pair.Value == null)
                {
                    continue;
                }
                var quanto = Vector2.Distance(
                    new Vector2(da.x, da.z), new Vector2(pair.Value.position.x, pair.Value.position.z));
                if (quanto < portata)
                {
                    return pair.Key;
                }
            }
            return "";
        }

        public void SpalancaLaPorta(string id)
        {
            if (_porte.TryGetValue(id, out var battente) && battente != null)
            {
                Destroy(battente.gameObject);
                _porte.Remove(id);
            }
        }

        public string DescrizioneDellaPorta(string id) => Porte.Descrizione(id);

        /// Quale corpo indossa ciascuno. Le mesh sono di Polytope Studio, e nel
        /// pacchetto gratuito i vestiti sono quattro in tutto: a distinguere
        /// dodici persone non e' il modello, e' `Panni` qui sotto.
        private static readonly Dictionary<string, string> Corpo = new Dictionary<string, string>
        {
            ["rosa"] = "PT_Female_Peasant_01_a", ["anna"] = "PT_Female_Peasant_01_b",
            ["laura"] = "PT_Female_Peasant_01_a", ["teresa"] = "PT_Female_Peasant_01_b",
            ["marisa"] = "PT_Female_Peasant_01_a", ["lidia"] = "PT_Female_Peasant_01_b",
            ["matteo"] = "PT_Male_Peasant_01", ["don_carlo"] = "PT_Male_Peasant_01",
            ["nino"] = "PT_Male_Peasant_01", ["piero"] = "PT_Male_Peasant_01",
            ["beppe"] = "PT_Male_Peasant_01", ["gino"] = "PT_Male_Peasant_01",
        };

        /// La stoffa e i capelli di ciascuno.
        ///
        /// Lo shader di Polytope tiene le tinte separate — pelle, capelli, quattro
        /// stoffe, quattro cuoi — quindi dodici persone si distinguono da lontano
        /// senza dodici modelli. E' anche l'unico posto del gioco in cui si puo'
        /// dire qualcosa di una persona senza che nessuno parli: **Anna e' vestita
        /// di nero dal 1966**, e chi lo nota lo ha capito da solo.
        ///
        /// Sono colori del 1987 in montagna, a ottobre: lana, grembiuli, tute da
        /// lavoro. Niente di saturo, o il paese sembra una fiera.
        private static readonly Dictionary<string, (Color Stoffa, Color Capelli)> Panni =
            new Dictionary<string, (Color, Color)>
        {
            ["anna"]      = (new Color(0.11f, 0.10f, 0.11f), new Color(0.62f, 0.60f, 0.57f)),
            ["don_carlo"] = (new Color(0.09f, 0.09f, 0.10f), new Color(0.72f, 0.70f, 0.66f)),
            ["rosa"]      = (new Color(0.42f, 0.38f, 0.42f), new Color(0.35f, 0.28f, 0.24f)),
            ["laura"]     = (new Color(0.28f, 0.31f, 0.38f), new Color(0.24f, 0.18f, 0.15f)),
            ["teresa"]    = (new Color(0.33f, 0.27f, 0.34f), new Color(0.84f, 0.83f, 0.80f)),
            ["marisa"]    = (new Color(0.45f, 0.53f, 0.58f), new Color(0.33f, 0.24f, 0.18f)),
            ["lidia"]     = (new Color(0.48f, 0.26f, 0.21f), new Color(0.21f, 0.16f, 0.14f)),
            ["matteo"]    = (new Color(0.40f, 0.30f, 0.20f), new Color(0.22f, 0.17f, 0.14f)),
            ["nino"]      = (new Color(0.26f, 0.30f, 0.21f), new Color(0.55f, 0.54f, 0.51f)),
            ["piero"]     = (new Color(0.36f, 0.35f, 0.33f), new Color(0.66f, 0.65f, 0.62f)),
            ["beppe"]     = (new Color(0.72f, 0.69f, 0.61f), new Color(0.58f, 0.55f, 0.50f)),
            ["gino"]      = (new Color(0.24f, 0.31f, 0.42f), new Color(0.28f, 0.21f, 0.17f)),
            ["giorgio"]   = (new Color(0.30f, 0.32f, 0.34f), new Color(0.25f, 0.19f, 0.16f)),
        };

        private void CostruisciLeFigure()
        {
            var gente = new GameObject("gente").transform;
            foreach (var pair in Corpo)
            {
                var posizione = World.ActorOf(pair.Key).Position;
                if (!posizione.HasValue)
                {
                    continue;
                }
                var figura = Figura(pair.Key, pair.Value);
                figura.name = pair.Key;
                figura.transform.SetParent(gente);
                var dove = InScena(posizione.Value);
                // I piedi sul suolo vero: il prato ondeggia, e una figura a quota
                // zero su un dosso affonda fino al ginocchio.
                dove.y = Scenografia.QuotaTerra(dove.x, dove.z);
                figura.transform.position = dove;
                _corpi[pair.Key] = figura.transform;
            }
        }

        /// Una persona, vestita come dice `Panni`. Se il modello non c'e' — import
        /// non riuscito, cartella spostata — resta una capsula: il gioco deve
        /// poter partire lo stesso, perche' la scena serve a provare le
        /// conversazioni, non i poligoni.
        public static GameObject Figura(string id, string modello)
        {
            var prefab = Resources.Load<GameObject>("paese/gente/" + modello);
            if (prefab == null)
            {
                var ripiego = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ripiego.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                return ripiego;
            }
            var figura = Instantiate(prefab);
            // I modelli sono in metri veri e la cella e' un metro: si lasciano
            // com'erano.
            figura.transform.localScale = Vector3.one;
            Vesti(figura, id);
            Posa(figura);

            var urto = figura.AddComponent<CapsuleCollider>();
            urto.height = 1.8f;
            urto.radius = 0.3f;
            urto.center = new Vector3(0f, 0.9f, 0f);
            return figura;
        }

        /// Le braccia giu'.
        ///
        /// Il pacchetto gratuito non porta nessuna animazione: senza un
        /// controller la figura resta nella posa di legatura, e se quella e' una
        /// T ci sono dodici persone in piazza a braccia aperte.
        ///
        /// Non si indovina come sia modellata: si guarda dove sta la mano. Se
        /// pende gia', non si tocca niente; se no, si gira la spalla finche' il
        /// braccio non punta in basso. Girare di tot gradi su un asse scelto a
        /// occhio funziona con una gabbia e non con la successiva — questo
        /// funziona con qualunque.
        private static void Posa(GameObject figura)
        {
            var ossa = new Dictionary<string, Transform>();
            foreach (var osso in figura.GetComponentsInChildren<Transform>())
            {
                ossa[osso.name] = osso;
            }
            Penzoloni(ossa, "PT_LeftArm", "PT_LeftHand", new Vector3(-0.22f, -1f, 0.06f));
            Penzoloni(ossa, "PT_RightArm", "PT_RightHand", new Vector3(0.22f, -1f, 0.06f));
        }

        private static void Penzoloni(
            Dictionary<string, Transform> ossa, string spalla, string mano, Vector3 voluta)
        {
            if (!ossa.TryGetValue(spalla, out var alto) || !ossa.TryGetValue(mano, out var basso))
            {
                return;
            }
            var braccio = basso.position - alto.position;
            if (braccio.sqrMagnitude < 0.0001f || braccio.normalized.y < -0.6f)
            {
                return;
            }
            alto.rotation = Quaternion.FromToRotation(braccio, voluta) * alto.rotation;
        }

        /// Le tinte di una persona sola.
        ///
        /// Il materiale si duplica per ciascuno: quello del pacchetto e' uno solo
        /// e condiviso, e cambiarlo in luogo vestirebbe tutto il paese uguale — e
        /// peggio, resterebbe cambiato sul disco anche dopo aver chiuso il gioco.
        private static void Vesti(GameObject figura, string id)
        {
            if (!Panni.TryGetValue(id, out var panni))
            {
                return;
            }
            foreach (var pezzo in figura.GetComponentsInChildren<Renderer>())
            {
                var suo = new Material(pezzo.sharedMaterial);
                // Lo shader di Polytope: se un giorno il modello cambia, queste
                // chiamate non trovano la proprieta' e non fanno niente. Nessuno
                // schianto, solo un paese vestito come l'ha lasciato l'autore.
                if (suo.HasProperty("_CLOTH1COLOR"))
                {
                    suo.SetColor("_CLOTH1COLOR", panni.Stoffa);
                    suo.SetColor("_CLOTH2COLOR", panni.Stoffa * 0.78f);
                    suo.SetColor("_CLOTH3COLOR", panni.Stoffa * 1.15f);
                    suo.SetColor("_CLOTH4COLOR", panni.Stoffa * 0.9f);
                }
                if (suo.HasProperty("_HAIRCOLOR"))
                {
                    suo.SetColor("_HAIRCOLOR", panni.Capelli);
                }
                pezzo.sharedMaterial = suo;
            }
        }

        /// Chi e' abbastanza vicino da poterci parlare.
        public string PiuVicino(Vector3 da, float portata)
        {
            var migliore = "";
            var distanza = portata;
            foreach (var pair in _corpi)
            {
                // Distanza a terra: uno alto e uno basso sono vicini uguale.
                var quanto = Vector2.Distance(
                    new Vector2(da.x, da.z), new Vector2(pair.Value.position.x, pair.Value.position.z));
                if (quanto < distanza)
                {
                    distanza = quanto;
                    migliore = pair.Key;
                }
            }
            return migliore;
        }
    }
}
