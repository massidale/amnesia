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
        public string Problema { get; private set; } = "";

        private readonly Dictionary<string, Transform> _corpi = new Dictionary<string, Transform>();
        private readonly Dictionary<string, string> _nomi = new Dictionary<string, string>();
        private DeclarationTable _dichiarazioni;

        /// Il taccuino si costruisce sul mondo di adesso e non si conserva: la
        /// sessione sostituisce il mondo a ogni turno riuscito, e un taccuino
        /// tenuto da parte leggerebbe la partita di due battute fa.
        public Taccuino Taccuino => new Taccuino(Session.World, _dichiarazioni, Items);

        private static string Contenuto(params string[] parti) =>
            Path.Combine(Application.streamingAssetsPath, Path.Combine(parti));

        private void Awake()
        {
            if (!Carica())
            {
                enabled = false;
                return;
            }
            CostruisciIlPaese();
            CostruisciLeFigure();
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
            foreach (var starting in new[] { "fotografia", "frase", "foglio_indirizzo", "chiave_b17", "taccuino" })
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

            var saluti = GreetingTable.Load(Contenuto("amnesia", "saluti.json"));
            if (!saluti.IsOk)
            {
                Problema = saluti.Message;
                return false;
            }
            _dichiarazioni = dichiarazioni.Value;
            Accoglienza = new Greeter(saluti.Value, posizioni.Value);

            var tabelle = new DeclarationService(dichiarazioni.Value, posizioni.Value);
            var contesto = new ContextBuilder(regole, schede, Items, true, dichiarazioni.Value, posizioni.Value);
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
                ("nino", "segheria"),
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

        private void CostruisciIlPaese()
        {
            Scenografia.Costruisci(Map, CellSize, new GameObject("paese").transform);
        }

        /// Quale corpo indossa ciascuno. Le mesh sono di Kenney (CC0); le
        /// facce le dipinge `tools/facce.py`, che tiene la stessa lista: se qui
        /// cambia una lettera, va cambiata anche la'.
        private static readonly Dictionary<string, string> Corpo = new Dictionary<string, string>
        {
            ["rosa"] = "e", ["anna"] = "e", ["laura"] = "e",
            ["matteo"] = "q", ["don_carlo"] = "j", ["nino"] = "a",
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
                figura.transform.position = InScena(posizione.Value);
                _corpi[pair.Key] = figura.transform;
            }
        }

        /// La mesh di Kenney con sopra la faccia dipinta per questa persona. Se
        /// il modello non c'e' — import non riuscito, cartella spostata — resta
        /// una capsula: il gioco deve poter partire lo stesso, perche' la scena
        /// serve a provare le conversazioni, non i poligoni.
        private static GameObject Figura(string id, string mesh)
        {
            var modello = Resources.Load<GameObject>("kenney/persone/character-" + mesh);
            if (modello == null)
            {
                var ripiego = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ripiego.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                return ripiego;
            }
            var figura = Instantiate(modello);
            figura.transform.localScale = Vector3.one * 0.65f;

            var faccia = Resources.Load<Texture2D>("kenney/persone/facce/" + id);
            if (faccia != null)
            {
                // Il materiale si assegna qui e non si eredita dal .mtl:
                // l'importatore OBJ di Unity a volte non trova la texture, e una
                // faccia bianca sarebbe un difetto invisibile finche' non lo
                // guardi in gioco.
                var materiale = new Material(Shader.Find("Standard")) { mainTexture = faccia };
                materiale.SetFloat("_Glossiness", 0f);
                foreach (var pezzo in figura.GetComponentsInChildren<Renderer>())
                {
                    pezzo.sharedMaterial = materiale;
                }
            }

            var urto = figura.AddComponent<CapsuleCollider>();
            urto.height = 2.7f;
            urto.radius = 0.5f;
            urto.center = new Vector3(0f, 1.35f, 0f);
            return figura;
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
