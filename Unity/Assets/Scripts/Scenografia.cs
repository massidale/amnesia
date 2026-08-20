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

        /// Le mesh comprate sono modellate in metri veri e la cella e' un metro,
        /// quindi la scala di partenza e' 1. Un castagno vero pero' e' alto tre
        /// volte una casa, e un paese di alberi giusti si guarda solo dall'alto:
        /// qui si rimpiccioliscono finche' le case restano la cosa piu' grande
        /// che si vede. Se una proporzione non convince, sono questi tre numeri.
        private const float ScalaAlberi = 0.55f;
        private const float ScalaSottobosco = 0.7f;
        private const float ScalaArredo = 1f;

        /// Quanto e' vestito il paese. Piu' alto, piu' vuoto: e' la frazione di
        /// celle a ridosso di un muro che restano nude.
        private const float SogliaArredo = 0.74f;

        private static readonly Color Strada = new Color(0.44f, 0.41f, 0.36f);
        private static readonly Color Prato = new Color(0.33f, 0.38f, 0.24f);
        private static readonly Color Pavimento = new Color(0.36f, 0.29f, 0.22f);
        private static readonly Color Soglia = new Color(0.30f, 0.23f, 0.17f);
        private static readonly Color Sottobosco = new Color(0.25f, 0.28f, 0.19f);
        private static readonly Color Acqua = new Color(0.24f, 0.32f, 0.36f);
        private static readonly Color Intonaco = new Color(0.62f, 0.56f, 0.47f);
        private static readonly Color Roccia = new Color(0.31f, 0.31f, 0.30f);
        private static readonly Color Bosco = new Color(0.22f, 0.30f, 0.20f);

        /// Ottobre in montagna: cielo chiuso, luce bassa, foschia che mangia il
        /// fondo valle. La nebbia non e' atmosfera, e' quello che impedisce di
        /// vedere il bordo della mappa.
        private static readonly Color Cielo = new Color(0.58f, 0.60f, 0.62f);

        public static void Costruisci(VillageMap mappa, float cella, Transform radice)
        {
            Cielo1987();
            Volta(mappa, cella, radice);
            Prateria(mappa, cella, radice);
            Terreno(mappa, cella, radice);
            Volumi(mappa, cella, radice);
            Vegetazione(mappa, cella, radice);
            Steccato(mappa, cella, radice);
            Arredo(mappa, cella, radice);
            Bordi(mappa, cella, radice);
        }

        /// Quattro pareti invisibili intorno al paese. Il terreno finisce dove
        /// finisce la mappa, e senza queste il giocatore che cammina verso il
        /// bordo esce dal mondo e cade: non e' una ringhiera, e' il pavimento
        /// che smette.
        private static void Bordi(VillageMap mappa, float cella, Transform radice)
        {
            var bordo = new GameObject("bordi").transform;
            bordo.SetParent(radice);
            var larghezza = mappa.Width * cella;
            var altezza = mappa.Height * cella;
            var centro = new Vector3(larghezza * 0.5f - cella * 0.5f, 6f, -altezza * 0.5f + cella * 0.5f);
            var spessore = cella;

            Parete(bordo, new Vector3(centro.x, centro.y, cella * 0.5f), new Vector3(larghezza, 12f, spessore));
            Parete(bordo, new Vector3(centro.x, centro.y, -altezza + cella * 0.5f), new Vector3(larghezza, 12f, spessore));
            Parete(bordo, new Vector3(-cella * 0.5f, centro.y, centro.z), new Vector3(spessore, 12f, altezza));
            Parete(bordo, new Vector3(larghezza - cella * 0.5f, centro.y, centro.z), new Vector3(spessore, 12f, altezza));
        }

        private static void Parete(Transform genitore, Vector3 dove, Vector3 misura)
        {
            var parete = new GameObject("parete");
            parete.transform.SetParent(genitore);
            parete.transform.position = dove;
            parete.AddComponent<BoxCollider>().size = misura;
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
            RenderSettings.fogDensity = 0.014f;
        }

        /// La cupola del cielo e qualche nuvola.
        ///
        /// La macchina da presa continua a pulire in tinta unita: la cupola e'
        /// geometria, non uno skybox, e le si stampa davanti. Il colore di fondo
        /// resta quello che c'era, cosi' se il modello non c'e' il cielo e' come
        /// prima invece che nero.
        private static void Volta(VillageMap mappa, float cella, Transform radice)
        {
            var largo = Mathf.Sqrt(mappa.Width * mappa.Width + mappa.Height * mappa.Height) * cella;
            var centro = new Vector3(mappa.Width * cella * 0.5f, 0f, -mappa.Height * cella * 0.5f);

            var cupola = Modello("cielo", "rpgpp_lt_sky_01", radice, 0, 0, cella);
            if (cupola != null)
            {
                cupola.name = "cielo";
                cupola.transform.position = centro;
                cupola.transform.rotation = Quaternion.identity;
                // La cupola si misura, non si indovina: un modello comprato puo'
                // essere alto un metro o duecento, e in un caso sparisce dentro
                // il paese, nell'altro finisce oltre il piano di taglio della
                // macchina da presa. Si guarda quanto e' grande e la si porta al
                // raggio che serve.
                cupola.transform.localScale = Vector3.one;
                var quanto = Raggio(cupola);
                if (quanto > 0.001f)
                {
                    cupola.transform.localScale = Vector3.one * (largo * 0.75f / quanto);
                }
                // Una cupola che proietta ombra fa notte in pieno giorno.
                foreach (var pezzo in cupola.GetComponentsInChildren<Renderer>())
                {
                    pezzo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    pezzo.receiveShadows = false;
                }
            }

            var nuvole = new GameObject("nuvole").transform;
            nuvole.SetParent(radice);
            for (var i = 0; i < 14; i++)
            {
                var nuvola = Modello("cielo", i % 2 == 0 ? "rpgpp_lt_cloud_01" : "rpgpp_lt_cloud_02",
                    nuvole, i * 7, i * 5, cella);
                if (nuvola == null)
                {
                    break;
                }
                nuvola.transform.position = centro + new Vector3(
                    (Caso(i, 3, 41) - 0.5f) * largo * 1.6f,
                    28f + Caso(i, 5, 43) * 22f,
                    (Caso(i, 7, 47) - 0.5f) * largo * 1.6f);
                nuvola.transform.localScale = Vector3.one * (8f + Caso(i, 11, 53) * 9f);
                foreach (var pezzo in nuvola.GetComponentsInChildren<Renderer>())
                {
                    pezzo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        /// Il prato che c'e' anche dove la mappa finisce.
        ///
        /// Prima il paese galleggiava: oltre il bordo non c'era niente, e a
        /// salvarlo era solo la nebbia. Un piano d'erba largo il doppio della
        /// mappa costa un quadrato di quattro vertici e toglie l'impressione che
        /// il mondo sia un tavolino.
        private static void Prateria(VillageMap mappa, float cella, Transform radice)
        {
            var margine = 70f;
            var larghezza = mappa.Width * cella;
            var altezza = mappa.Height * cella;

            var falda = new Falda();
            falda.Quadrato(larghezza * 0.5f, -0.04f, -altezza * 0.5f,
                Mathf.Max(larghezza, altezza) + margine * 2f);
            var piano = new GameObject("prato");
            piano.transform.SetParent(radice);
            var mesh = falda.Mesh();
            piano.AddComponent<MeshFilter>().sharedMesh = mesh;
            piano.AddComponent<MeshRenderer>().sharedMaterial = Materiale(Prato);
            piano.AddComponent<MeshCollider>().sharedMesh = mesh;

            var ciuffi = new GameObject("erba").transform;
            ciuffi.SetParent(radice);
            // Dentro il paese l'erba va sull'erba: sulla strada no, o si cammina
            // in mezzo ai cespugli.
            for (var y = 0; y < mappa.Height; y++)
            {
                for (var x = 0; x < mappa.Width; x++)
                {
                    if (mappa.Rows[y][x] != ',' || Caso(x, y, 59) < 0.35f)
                    {
                        continue;
                    }
                    var ciuffo = Modello("prato", Scegli(Erba, x, y, 61), ciuffi, x, y, cella);
                    if (ciuffo != null)
                    {
                        ciuffo.transform.localScale = Vector3.one * (0.8f + Caso(x, y, 67) * 0.7f);
                    }
                }
            }
            // E fuori dal bordo, dove non si arriva ma si vede.
            for (var i = 0; i < 700; i++)
            {
                var x = (int)((Caso(i, 2, 71) * 2f - 0.5f) * mappa.Width);
                var y = (int)((Caso(i, 4, 73) * 2f - 0.5f) * mappa.Height);
                if (x >= 0 && x < mappa.Width && y >= 0 && y < mappa.Height)
                {
                    continue;
                }
                var fuori = Modello("prato", Scegli(Erba, i, i, 79), ciuffi, x, y, cella);
                if (fuori == null)
                {
                    break;
                }
                fuori.transform.localScale = Vector3.one * (1f + Caso(i, 6, 83) * 1.4f);
            }
        }

        /// Gli steccati fra l'orto e la strada.
        private static void Steccato(VillageMap mappa, float cella, Transform radice)
        {
            var recinti = new GameObject("steccati").transform;
            recinti.SetParent(radice);

            for (var y = 1; y < mappa.Height - 1; y++)
            {
                for (var x = 1; x < mappa.Width - 1; x++)
                {
                    if (mappa.Rows[y][x] != ',' || Caso(x, y, 89) < 0.42f || AccantoAUnaPorta(mappa, x, y))
                    {
                        continue;
                    }
                    // Lo steccato sta sul confine fra l'erba e la strada, e guarda
                    // la strada: e' un recinto, e un recinto ha un dentro.
                    float giro;
                    if (mappa.Rows[y][x + 1] == '.') { giro = 0f; }
                    else if (mappa.Rows[y][x - 1] == '.') { giro = 180f; }
                    else if (mappa.Rows[y + 1][x] == '.') { giro = 90f; }
                    else if (mappa.Rows[y - 1][x] == '.') { giro = 270f; }
                    else { continue; }

                    var palo = Modello("steccato", Scegli(Steccati, x, y, 97), recinti, x, y, cella);
                    if (palo != null)
                    {
                        palo.transform.position = new Vector3(x * cella, 0f, -y * cella);
                        palo.transform.rotation = Quaternion.Euler(0f, giro, 0f);
                    }
                }
            }
        }

        public static Color ColoreDelCielo => Cielo;

        /// Il colore di un simbolo della mappa. Lo chiedono in due — il paese in
        /// tre dimensioni e la pianta nel menu — e devono rispondere uguale, o
        /// la carta che il giocatore guarda non e' del posto in cui cammina.
        public static Color ColoreDi(char simbolo)
        {
            switch (simbolo)
            {
                case ',': return Prato;
                case '~': return Pavimento;
                case '+': return Soglia;
                case '"': return Sottobosco;
                case '=': return Acqua;
                case '#': return Intonaco;
                case '^': return Roccia;
                case 'T': return Bosco;
                default: return Strada;
            }
        }

        private static void Terreno(VillageMap mappa, float cella, Transform radice)
        {
            var quote = new Dictionary<char, float>
            {
                ['.'] = 0f, [','] = 0f, ['~'] = 0.02f, ['+'] = 0.02f, ['"'] = 0f, ['='] = -0.25f,
            };
            var falde = new Dictionary<char, Falda>();

            for (var y = 0; y < mappa.Height; y++)
            {
                for (var x = 0; x < mappa.Width; x++)
                {
                    var simbolo = mappa.Rows[y][x];
                    // Sotto i muri e la montagna il terreno c'e' lo stesso: senza,
                    // ogni porta si aprirebbe sul vuoto.
                    var chiave = quote.ContainsKey(simbolo) ? simbolo : '.';
                    if (!falde.TryGetValue(chiave, out var falda))
                    {
                        falda = new Falda();
                        falde[chiave] = falda;
                    }
                    falda.Quadrato(x * cella, quote[chiave], -y * cella, cella);
                }
            }

            foreach (var pair in falde)
            {
                var go = new GameObject($"terreno {pair.Key}");
                go.transform.SetParent(radice);
                var mesh = pair.Value.Mesh();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = Materiale(ColoreDi(pair.Key));
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
                        // Un masso appoggiato sopra, ogni tanto. E' quello che
                        // trasforma una fila di cubi grigi in una parete di
                        // roccia, e sono le prime cose che si vedono alzando gli
                        // occhi da qualunque punto del paese.
                        if (Caso(x, y, 101) > 0.62f)
                        {
                            var masso = Modello("roccia", Scegli(Massi, x, y, 103), monte, x, y, cella);
                            if (masso != null)
                            {
                                masso.transform.position += Vector3.up * scarto;
                                masso.transform.localScale = Vector3.one * (1.1f + Caso(x, y, 107) * 1.6f);
                            }
                        }
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

        /// Il bosco. In alto, dove la valle si stringe, sono pini; piu' giu' il
        /// castagneto. Niente alberi secchi: un bosco spoglio si legge come un
        /// posto morto, e questo paese e' pieno di gente che ci vive.
        private static readonly string[] Pini = { "PT_Pine_Tree_03_green" };

        private static readonly string[] Castagni =
        {
            "PT_Fruit_Tree_01_green", "PT_Fruit_Tree_01_apples", "PT_Fruit_Tree_01_plums",
        };

        private static readonly string[] Cespugli =
        {
            "PT_Generic_Shrub_01_green",
            "PT_High_Grass_02_v1", "PT_Grass_02", "PT_Grass_02_v1", "PT_Poppy_02",
        };

        /// Il prato: ciuffi d'erba, cespugli, qualche fiore. Va sull'erba dentro
        /// il paese e su tutto quello che sta fuori dalla mappa, che altrimenti e'
        /// un piano verde e basta.
        private static readonly string[] Erba =
        {
            "rpgpp_lt_grass_small_01a", "rpgpp_lt_grass_small_01b",
            "rpgpp_lt_bush_01", "rpgpp_lt_bush_02", "rpgpp_lt_flower_03",
            "rpgpp_lt_plant_01", "rpgpp_lt_plant_02",
            "rpgpp_lt_terrain_grass_01", "rpgpp_lt_terrain_grass_02",
        };

        /// I sassi che si appoggiano sopra la montagna. Un cubo grigio alto sei
        /// metri e' un muro; lo stesso cubo con tre massi sopra e' una parete di
        /// roccia, ed e' l'unica differenza fra le due cose.
        private static readonly string[] Massi =
        {
            "rpgpp_lt_rock_01", "rpgpp_lt_rock_02", "rpgpp_lt_rock_03",
            "rpgpp_lt_rock_small_01", "rpgpp_lt_rock_small_02", "rpgpp_lt_rocks_tiny_01",
            "rpgpp_lt_hill_small_01", "rpgpp_lt_hill_small_02",
        };

        private static readonly string[] Steccati =
        {
            "rpgpp_lt_fence_wood_01a", "rpgpp_lt_fence_wood_01b",
            "rpgpp_lt_fence_wood_02a", "rpgpp_lt_fence_wood_02b", "rpgpp_lt_fence_wood_02c",
        };

        /// Roba di gente che lavora, non decorazione: botti, casse, sacchi, una
        /// scala appoggiata al muro. Serve a una cosa sola e non estetica — un
        /// paese in cui ogni casa e' un cubo liscio non ha punti di riferimento,
        /// e senza punti di riferimento «il castagneto sopra la curva» non vuol
        /// dire niente. La geografia del quarto atto vive di questo.
        private static readonly string[] Arredi =
        {
            "rpgpp_lt_barrel_01", "rpgpp_lt_barrel_02", "rpgpp_lt_crate_01", "rpgpp_lt_crate_02",
            "rpgpp_lt_crate_03", "rpgpp_lt_sack_01", "rpgpp_lt_sack_02", "rpgpp_lt_sack_02_set",
            "rpgpp_lt_basket_01", "rpgpp_lt_basket_02", "rpgpp_lt_bench_wood_01",
            "rpgpp_lt_bench_wood_02", "rpgpp_lt_box_wood_01", "rpgpp_lt_log_wood_01",
            "rpgpp_lt_log_wood_02a", "rpgpp_lt_bucket_01", "rpgpp_lt_vase_01", "rpgpp_lt_vase_02",
            "rpgpp_lt_jug_01", "rpgpp_lt_trough_01", "rpgpp_lt_ladder_01", "rpgpp_lt_package_01",
            "rpgpp_lt_hanger_clothes_01", "rpgpp_lt_rake_01", "rpgpp_lt_broom_01",
            "rpgpp_lt_stones_01", "rpgpp_lt_flower_01", "rpgpp_lt_flower_02",
        };

        /// Un oggetto solo per luogo, e ognuno dice che mestiere ci si fa. Il
        /// pozzo in piazza e' anche l'unica cosa del paese che si vede da lontano
        /// e che non e' una casa: e' li' che uno si orienta.
        private static readonly Dictionary<string, string> Insegne = new Dictionary<string, string>
        {
            ["piazza"] = "rpgpp_lt_well_01",
            ["deposito"] = "rpgpp_lt_wagon_01",
            ["panetteria"] = "rpgpp_lt_awning_standing_01a",
            ["negozio"] = "rpgpp_lt_awning_standing_01b",
            ["bar"] = "rpgpp_lt_banner_01a",
            ["bottega"] = "rpgpp_lt_shed_wood_01",
            ["giardino"] = "rpgpp_lt_bird_house_01",
        };

        private static void Vegetazione(VillageMap mappa, float cella, Transform radice)
        {
            var bosco = new GameObject("bosco").transform;
            bosco.SetParent(radice);
            // La cava sta in cima alla mappa: sopra il primo terzo e' quota, e a
            // quella quota crescono conifere.
            var quota = mappa.Height / 3;

            for (var y = 0; y < mappa.Height; y++)
            {
                for (var x = 0; x < mappa.Width; x++)
                {
                    var simbolo = mappa.Rows[y][x];
                    if (simbolo == 'T')
                    {
                        var specie = y < quota ? Pini : Castagni;
                        var albero = Modello("natura", Scegli(specie, x, y, 3), bosco, x, y, cella);
                        if (albero != null)
                        {
                            albero.transform.localScale =
                                Vector3.one * ScalaAlberi * (0.85f + Caso(x, y, 11) * 0.45f);
                            // Il tronco si scontra, la chioma no: un bosco che si
                            // attraversa non chiude niente, e la cava deve essere
                            // difficile da raggiungere.
                            var tronco = albero.AddComponent<CapsuleCollider>();
                            tronco.radius = 0.18f;
                            tronco.height = 3f;
                            tronco.center = new Vector3(0f, 1.5f, 0f);
                        }
                    }
                    else if (simbolo == '"' && Caso(x, y, 5) > 0.45f)
                    {
                        // Il sottobosco non si scontra: ci si cammina dentro.
                        var cespuglio = Modello("natura", Scegli(Cespugli, x, y, 13), bosco, x, y, cella);
                        if (cespuglio != null)
                        {
                            cespuglio.transform.localScale =
                                Vector3.one * ScalaSottobosco * (0.8f + Caso(x, y, 17) * 0.6f);
                        }
                    }
                }
            }
        }

        /// Le cose appoggiate ai muri, e un oggetto per luogo che ne dica il
        /// mestiere.
        private static void Arredo(VillageMap mappa, float cella, Transform radice)
        {
            var arredo = new GameObject("arredo").transform;
            arredo.SetParent(radice);

            for (var y = 1; y < mappa.Height - 1; y++)
            {
                for (var x = 1; x < mappa.Width - 1; x++)
                {
                    if (!Calpestabile(mappa, x, y) || Caso(x, y, 31) < SogliaArredo)
                    {
                        continue;
                    }
                    // Solo a ridosso di una parete, e mai davanti a una porta:
                    // una cassa su una soglia e' un dettaglio grazioso il giorno
                    // che lo metti e un ostacolo per tutta la partita.
                    if (!ControIlMuro(mappa, x, y, out var verso) || AccantoAUnaPorta(mappa, x, y))
                    {
                        continue;
                    }
                    var cosa = Modello("arredo", Scegli(Arredi, x, y, 37), arredo, x, y, cella);
                    if (cosa != null)
                    {
                        cosa.transform.position += new Vector3(verso.x, 0f, verso.y) * cella * 0.33f;
                        cosa.transform.localScale = Vector3.one * ScalaArredo;
                    }
                }
            }

            foreach (var pair in Insegne)
            {
                var centro = mappa.CenterOf(pair.Key);
                if (centro is { } dove)
                {
                    Modello("insegne", pair.Value, arredo, dove.X, dove.Y, cella);
                }
            }
        }

        private static bool Calpestabile(VillageMap mappa, int x, int y)
        {
            var simbolo = mappa.Rows[y][x];
            return simbolo == '.' || simbolo == ',';
        }

        /// La direzione del muro piu' vicino, se ce n'e' uno di fianco.
        private static bool ControIlMuro(VillageMap mappa, int x, int y, out Vector2 verso)
        {
            if (mappa.Rows[y][x - 1] == '#') { verso = new Vector2(-1f, 0f); return true; }
            if (mappa.Rows[y][x + 1] == '#') { verso = new Vector2(1f, 0f); return true; }
            if (mappa.Rows[y - 1][x] == '#') { verso = new Vector2(0f, 1f); return true; }
            if (mappa.Rows[y + 1][x] == '#') { verso = new Vector2(0f, -1f); return true; }
            verso = Vector2.zero;
            return false;
        }

        private static bool AccantoAUnaPorta(VillageMap mappa, int x, int y)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (mappa.Rows[y + dy][x + dx] == '+')
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// Quanto e' grande un modello, in metri, cosi' com'e' uscito dal
        /// pacchetto. Serve al cielo, che e' l'unica cosa della scena la cui
        /// misura giusta non dipende da noi ma da chi l'ha modellata.
        private static float Raggio(GameObject cosa)
        {
            var pezzi = cosa.GetComponentsInChildren<Renderer>();
            if (pezzi.Length == 0)
            {
                return 0f;
            }
            var tutto = pezzi[0].bounds;
            for (var i = 1; i < pezzi.Length; i++)
            {
                tutto.Encapsulate(pezzi[i].bounds);
            }
            return Mathf.Max(tutto.extents.x, tutto.extents.z);
        }

        private static string Scegli(string[] fra, int x, int y, int seme) =>
            fra[(int)(Caso(x, y, seme) * fra.Length) % fra.Length];

        /// Un prefab da `Resources/paese/<cartella>`, piantato sulla sua cella con
        /// un filo di scarto e una rotazione qualunque — sempre gli stessi, pero':
        /// «l'albero davanti alla bottega» deve voler dire qualcosa anche domani.
        private static GameObject Modello(
            string cartella, string nome, Transform genitore, int x, int y, float cella)
        {
            var prefab = Resources.Load<GameObject>("paese/" + cartella + "/" + nome);
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
