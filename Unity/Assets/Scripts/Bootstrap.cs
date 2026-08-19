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
        public string Problema { get; private set; } = "";

        private readonly Dictionary<string, Transform> _corpi = new Dictionary<string, Transform>();
        private readonly Dictionary<string, string> _nomi = new Dictionary<string, string>();

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
                var centro = Map.CenterOf(pair.Item2);
                if (centro.HasValue)
                {
                    World.ActorOf(pair.Item1).Position = centro.Value;
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

        /// Un cubo per ogni cella solida. Brutto e sufficiente: serve a capire se
        /// una conversazione regge come momento di gioco, non a fare un paese.
        private void CostruisciIlPaese()
        {
            var terra = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terra.name = "terra";
            terra.transform.localScale = new Vector3(Map.Width * CellSize / 10f, 1f, Map.Height * CellSize / 10f);
            terra.transform.position = new Vector3(Map.Width * CellSize / 2f, 0f, -Map.Height * CellSize / 2f);
            terra.GetComponent<Renderer>().material.color = new Color(0.42f, 0.40f, 0.36f);

            var muri = new GameObject("muri").transform;
            for (var y = 0; y < Map.Height; y++)
            {
                for (var x = 0; x < Map.Width; x++)
                {
                    if (Map.IsWalkable(new Cell(x, y)))
                    {
                        continue;
                    }
                    var blocco = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blocco.transform.SetParent(muri);
                    blocco.transform.position = InScena(new Cell(x, y)) + Vector3.up * (CellSize * 0.5f);
                    blocco.transform.localScale = Vector3.one * CellSize;
                    blocco.GetComponent<Renderer>().material.color = new Color(0.30f, 0.28f, 0.26f);
                }
            }

            var sole = new GameObject("sole").AddComponent<Light>();
            sole.type = LightType.Directional;
            sole.transform.rotation = Quaternion.Euler(38f, 205f, 0f);
            sole.color = new Color(1f, 0.94f, 0.82f);
            sole.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.38f);
        }

        private void CostruisciLeFigure()
        {
            foreach (var id in new[] { "rosa", "matteo", "anna", "laura", "don_carlo", "nino" })
            {
                var posizione = World.ActorOf(id).Position;
                if (!posizione.HasValue)
                {
                    continue;
                }
                var figura = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                figura.name = id;
                figura.transform.position = InScena(posizione.Value) + Vector3.up * CellSize;
                figura.transform.localScale = new Vector3(CellSize * 0.7f, CellSize, CellSize * 0.7f);
                figura.GetComponent<Renderer>().material.color = new Color(0.72f, 0.62f, 0.50f);
                _corpi[id] = figura.transform;
            }
        }

        /// Chi e' abbastanza vicino da poterci parlare.
        public string PiuVicino(Vector3 da, float portata)
        {
            var migliore = "";
            var distanza = portata;
            foreach (var pair in _corpi)
            {
                var quanto = Vector3.Distance(da, pair.Value.position);
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
