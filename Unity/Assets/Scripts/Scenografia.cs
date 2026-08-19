using System.Collections.Generic;
using Amnesia.Core;
using Amnesia.World;
using UnityEngine;

namespace AmnesiaUnity
{
    /// Il paese come si vede. La mappa resta un file di caratteri — questa e'
    /// solo la sua lettura ad alta voce: '#' e' un muro, 'T' un castagno, '^' la
    /// montagna che chiude la valle.
    ///
    /// Il terreno e' una mesh sola per tipo invece di millecinquecento oggetti:
    /// non e' ottimizzazione prematura, e' che una scena con millecinquecento
    /// figli non si ispeziona piu' a mano quando qualcosa non torna.
    public static class Scenografia
    {
        private const float AltezzaMuro = 2.8f;
        private const float AltezzaMontagna = 5.5f;

        private static readonly Color Strada = new Color(0.44f, 0.41f, 0.36f);
        private static readonly Color Prato = new Color(0.33f, 0.38f, 0.24f);
        private static readonly Color Pavimento = new Color(0.36f, 0.29f, 0.22f);
        private static readonly Color Soglia = new Color(0.30f, 0.23f, 0.17f);
        private static readonly Color Sottobosco = new Color(0.25f, 0.28f, 0.19f);
        private static readonly Color Acqua = new Color(0.24f, 0.32f, 0.36f);
        private static readonly Color Intonaco = new Color(0.62f, 0.56f, 0.47f);
        private static readonly Color Roccia = new Color(0.31f, 0.31f, 0.30f);

        /// Ottobre in montagna: cielo chiuso, luce bassa, foschia che mangia il
        /// fondo valle. La nebbia non e' atmosfera, e' quello che impedisce di
        /// vedere il bordo della mappa.
        private static readonly Color Cielo = new Color(0.58f, 0.60f, 0.62f);

        public static void Costruisci(VillageMap mappa, float cella, Transform radice)
        {
            Cielo1987();
            Terreno(mappa, cella, radice);
            Volumi(mappa, cella, radice);
            Vegetazione(mappa, cella, radice);
        }

        private static void Cielo1987()
        {
            var sole = new GameObject("sole").AddComponent<Light>();
            sole.type = LightType.Directional;
            sole.transform.rotation = Quaternion.Euler(28f, 205f, 0f);
            sole.color = new Color(1f, 0.96f, 0.88f);
            sole.intensity = 0.95f;
            sole.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.49f, 0.54f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.35f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.19f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Cielo;
            RenderSettings.fogDensity = 0.018f;
        }

        public static Color ColoreDelCielo => Cielo;

        private static void Terreno(VillageMap mappa, float cella, Transform radice)
        {
            var suoli = new Dictionary<char, (Color colore, float quota)>
            {
                ['.'] = (Strada, 0f),
                [','] = (Prato, 0f),
                ['~'] = (Pavimento, 0.02f),
                ['+'] = (Soglia, 0.02f),
                ['"'] = (Sottobosco, 0f),
                ['='] = (Acqua, -0.25f),
            };
            var falde = new Dictionary<char, Falda>();

            for (var y = 0; y < mappa.Height; y++)
            {
                for (var x = 0; x < mappa.Width; x++)
                {
                    var simbolo = mappa.Rows[y][x];
                    // Sotto i muri e la montagna il terreno c'e' lo stesso: senza,
                    // ogni porta si aprirebbe sul vuoto.
                    var chiave = suoli.ContainsKey(simbolo) ? simbolo : '.';
                    if (!falde.TryGetValue(chiave, out var falda))
                    {
                        falda = new Falda();
                        falde[chiave] = falda;
                    }
                    falda.Quadrato(x * cella, suoli[chiave].quota, -y * cella, cella);
                }
            }

            foreach (var pair in falde)
            {
                var go = new GameObject($"terreno {pair.Key}");
                go.transform.SetParent(radice);
                var mesh = pair.Value.Mesh();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = Materiale(suoli[pair.Key].colore);
                // Il terreno si vede *e* si calpesta. Senza questo il giocatore
                // parte, la gravita' lo prende, e precipita attraverso il paese.
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
        }

        private static void Volumi(VillageMap mappa, float cella, Transform radice)
        {
            var muri = new GameObject("muri").transform;
            var monte = new GameObject("montagna").transform;
            muri.SetParent(radice);
            monte.SetParent(radice);
            var intonaco = Materiale(Intonaco);
            var pietra = Materiale(Roccia);

            for (var y = 0; y < mappa.Height; y++)
            {
                for (var x = 0; x < mappa.Width; x++)
                {
                    var simbolo = mappa.Rows[y][x];
                    if (simbolo == '#')
                    {
                        Blocco(muri, intonaco, x, y, cella, AltezzaMuro);
                    }
                    else if (simbolo == '^')
                    {
                        // Un filo di dislivello per cella: una parete di roccia
                        // perfettamente piatta si legge come un muro, non come
                        // una montagna.
                        var scarto = AltezzaMontagna * (0.75f + 0.5f * Caso(x, y, 7));
                        Blocco(monte, pietra, x, y, cella, scarto);
                    }
                    else if (simbolo == '=')
                    {
                        // L'acqua si vede ma non si attraversa.
                        var sbarra = new GameObject("acqua");
                        sbarra.transform.SetParent(monte);
                        sbarra.transform.position = new Vector3(x * cella, 0.5f, -y * cella);
                        sbarra.AddComponent<BoxCollider>().size = new Vector3(cella, 1.6f, cella);
                    }
                }
            }
        }

        private static void Blocco(Transform genitore, Material materiale, int x, int y, float cella, float altezza)
        {
            var blocco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocco.transform.SetParent(genitore);
            blocco.transform.position = new Vector3(x * cella, altezza * 0.5f, -y * cella);
            blocco.transform.localScale = new Vector3(cella, altezza, cella);
            blocco.GetComponent<Renderer>().sharedMaterial = materiale;
        }

        private static void Vegetazione(VillageMap mappa, float cella, Transform radice)
        {
            var bosco = new GameObject("bosco").transform;
            bosco.SetParent(radice);
            var castagni = new[] { "tree_default", "tree_detailed", "tree_blocks" };
            var cespugli = new[] { "grass", "grass_large", "stump_round" };

            for (var y = 0; y < mappa.Height; y++)
            {
                for (var x = 0; x < mappa.Width; x++)
                {
                    var simbolo = mappa.Rows[y][x];
                    if (simbolo == 'T')
                    {
                        var albero = Pianta(castagni[(int)(Caso(x, y, 3) * castagni.Length) % castagni.Length], bosco, x, y, cella);
                        if (albero != null)
                        {
                            albero.transform.localScale = Vector3.one * (2.4f + Caso(x, y, 11) * 0.9f);
                            var tronco = albero.AddComponent<CapsuleCollider>();
                            tronco.radius = 0.16f;
                            tronco.height = 3f;
                            tronco.center = new Vector3(0f, 1.5f, 0f);
                        }
                    }
                    else if (simbolo == '"' && Caso(x, y, 5) > 0.55f)
                    {
                        // Il sottobosco non si scontra: ci si cammina dentro.
                        var cespuglio = Pianta(cespugli[(int)(Caso(x, y, 13) * cespugli.Length) % cespugli.Length], bosco, x, y, cella);
                        if (cespuglio != null)
                        {
                            cespuglio.transform.localScale = Vector3.one * (1.2f + Caso(x, y, 17) * 0.6f);
                        }
                    }
                }
            }
        }

        private static GameObject Pianta(string modello, Transform genitore, int x, int y, float cella)
        {
            var prefab = Resources.Load<GameObject>("kenney/natura/" + modello);
            if (prefab == null)
            {
                return null;
            }
            var istanza = Object.Instantiate(prefab, genitore);
            istanza.transform.position = new Vector3(
                (x + Caso(x, y, 19) * 0.4f - 0.2f) * cella, 0f, -(y + Caso(x, y, 23) * 0.4f - 0.2f) * cella);
            istanza.transform.rotation = Quaternion.Euler(0f, Caso(x, y, 29) * 360f, 0f);
            return istanza;
        }

        public static Material Materiale(Color colore)
        {
            var materiale = new Material(Shader.Find("Standard"));
            materiale.color = colore;
            materiale.SetFloat("_Glossiness", 0.05f);
            return materiale;
        }

        /// Varieta' ripetibile. `Random` darebbe un paese diverso a ogni avvio, e
        /// un paese che cambia mentre lo si prova non si puo' descrivere a
        /// nessuno: «l'albero davanti alla bottega» deve voler dire qualcosa.
        private static float Caso(int x, int y, int seme)
        {
            var h = (x * 73856093) ^ (y * 19349663) ^ (seme * 83492791);
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }

        /// Una mesh sola, costruita quadrato per quadrato.
        private sealed class Falda
        {
            private readonly List<Vector3> _punti = new List<Vector3>();
            private readonly List<int> _triangoli = new List<int>();

            public void Quadrato(float x, float y, float z, float lato)
            {
                var m = lato * 0.5f;
                var b = _punti.Count;
                _punti.Add(new Vector3(x - m, y, z - m));
                _punti.Add(new Vector3(x - m, y, z + m));
                _punti.Add(new Vector3(x + m, y, z + m));
                _punti.Add(new Vector3(x + m, y, z - m));
                _triangoli.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }

            public Mesh Mesh()
            {
                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(_punti);
                mesh.SetTriangles(_triangoli, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
