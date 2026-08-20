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
        /// Il raggio della cupola, in diagonali di mappa.
        ///
        /// DEVE stare sopra 1, e il motivo e' costato una sera: a 0.6 la cupola
        /// aveva un raggio di sessantaquattro metri su una mappa che ne misura
        /// centosei di diagonale, quindi tagliava dentro il paese. Da un capo si
        /// vedeva un muro bianco al posto dell'altro capo, e attraversandolo le
        /// case comparivano. Il cielo va tenuto piu' lontano del punto piu'
        /// lontano, sempre.
        private const float ScalaCielo = 1.35f;

        private static readonly Color Strada = new Color(0.44f, 0.41f, 0.36f);
        private static readonly Color Prato = new Color(0.33f, 0.38f, 0.24f);
        private static readonly Color Pavimento = new Color(0.36f, 0.29f, 0.22f);
        private static readonly Color Soglia = new Color(0.30f, 0.23f, 0.17f);
        private static readonly Color Sottobosco = new Color(0.25f, 0.28f, 0.19f);
        private static readonly Color Acqua = new Color(0.24f, 0.32f, 0.36f);
        private static readonly Color Intonaco = new Color(0.62f, 0.56f, 0.47f);
        private static readonly Color Roccia = new Color(0.31f, 0.31f, 0.30f);
        private static readonly Color Bosco = new Color(0.22f, 0.30f, 0.20f);

        /// Le lose: in queste valli i tetti sono di pietra grigia, non di coppi.
        private static readonly Color Lose = new Color(0.29f, 0.28f, 0.27f);

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
            Tetti(mappa, cella, radice);
            Stanze(mappa, cella, radice);
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
            RenderSettings.fogDensity = 0.006f;
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
                    cupola.transform.localScale = Vector3.one * (largo * ScalaCielo / quanto);
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
                        if (Caso(x, y, 101) > 0.84f)
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
            // Niente `terrain_*` qui: quelle sono mattonelle di terreno, piani
            // larghi da posare a scacchiera, non ciuffi da piantare su una cella.
            // Sparse una per casella finivano a filo del prato e sfarfallavano —
            // due superfici alla stessa quota che si contendono lo stesso pixel.
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

        /// Un oggetto per luogo, che ne dica il mestiere. Sta FUORI, di fianco
        /// alla porta, come stanno le insegne: metterlo al centro del luogo — che
        /// e' quello che facevo — voleva dire piantare una tenda dentro la
        /// panetteria e uno stendardo addosso a Lidia, e in mezzo alla stanza un
        /// telo bianco e' esattamente cio' che il giocatore attraversa senza
        /// capire cos'era.
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

        /// Cosa c'e' dentro ciascuna stanza. Non e' decorazione: e' l'unico modo
        /// che ha una stanza di dire che mestiere ci si fa, e il giocatore ci
        /// entra per parlare con qualcuno — quello che vede intorno alla persona
        /// e' meta' di quello che sa di lei prima che apra bocca.
        private static readonly Dictionary<string, string[]> Mobilia =
            new Dictionary<string, string[]>
        {
            ["bottega"] = new[]
            {
                "rpgpp_lt_table_01", "rpgpp_lt_log_wood_02b", "rpgpp_lt_crate_01",
                "rpgpp_lt_ladder_01", "rpgpp_lt_box_wood_01", "rpgpp_lt_bucket_01",
            },
            ["panetteria"] = new[]
            {
                "rpgpp_lt_table_01", "rpgpp_lt_sack_open_01", "rpgpp_lt_sack_02",
                "rpgpp_lt_basket_01", "rpgpp_lt_basket_02", "rpgpp_lt_bowl_metal_01",
            },
            ["negozio"] = new[]
            {
                "rpgpp_lt_crate_02", "rpgpp_lt_crate_03", "rpgpp_lt_sack_01",
                "rpgpp_lt_basket_02", "rpgpp_lt_vase_03", "rpgpp_lt_jug_01",
            },
            ["bar"] = new[]
            {
                "rpgpp_lt_table_01", "rpgpp_lt_chair_01a", "rpgpp_lt_chair_01b",
                "rpgpp_lt_bench_wood_03", "rpgpp_lt_jug_01", "rpgpp_lt_bowl_metal_01",
            },
            ["chiesa"] = new[]
            {
                "rpgpp_lt_bench_wood_03", "rpgpp_lt_bench_wood_01", "rpgpp_lt_bench_wood_02",
            },
            ["canonica"] = new[]
            {
                "rpgpp_lt_table_01", "rpgpp_lt_chair_01a", "rpgpp_lt_bench_wood_01",
                "rpgpp_lt_vase_03",
            },
            ["stazione"] = new[]
            {
                "rpgpp_lt_bench_wood_01", "rpgpp_lt_bench_wood_02", "rpgpp_lt_crate_01",
                "rpgpp_lt_package_01",
            },
            ["deposito"] = new[]
            {
                "rpgpp_lt_crate_01", "rpgpp_lt_crate_02", "rpgpp_lt_barrel_01",
                "rpgpp_lt_barrel_02", "rpgpp_lt_sack_02_set", "rpgpp_lt_package_01",
            },
        };

        /// Le case in cui si abita. Stessa roba per tutte e tre, e va bene cosi':
        /// in un paese di montagna nel 1987 le cucine si somigliavano.
        private static readonly string[] Casa =
        {
            "rpgpp_lt_table_01", "rpgpp_lt_chair_01a", "rpgpp_lt_chair_01b",
            "rpgpp_lt_hanger_wood_01", "rpgpp_lt_hanger_wood_02", "rpgpp_lt_vase_03",
            "rpgpp_lt_plant_01", "rpgpp_lt_plant_02", "rpgpp_lt_bowl_metal_01",
        };

        /// Un oggetto per luogo, e nient'altro.
        ///
        /// Prima ne spargeva una sessantina lungo tutti i muri del paese, e il
        /// risultato era che non si vedeva piu' niente: quando ogni angolo ha una
        /// cassa, nessuna cassa vuol dire piu' niente. Un pozzo in piazza e un
        /// carro al deposito si ricordano; sessanta botti sono rumore.
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

        private static void Arredo(VillageMap mappa, float cella, Transform radice)
        {
            var arredo = new GameObject("arredo").transform;
            arredo.SetParent(radice);
            foreach (var pair in Insegne)
            {
                if (!mappa.HasPlace(pair.Key))
                {
                    continue;
                }
                var luogo = mappa.Places[pair.Key];
                // Fuori dalla porta se il luogo e' una stanza; al centro se e' uno
                // spiazzo, che e' il caso del pozzo e del giardino.
                var dove = SullUscio(mappa, luogo) ?? mappa.CenterOf(pair.Key);
                if (dove is { } cella2)
                {
                    Modello("insegne", pair.Value, arredo, cella2.X, cella2.Y, cella);
                }
            }
        }

        /// La cella calpestabile subito fuori dalla porta di un luogo, se ne ha
        /// una. Nullo per gli spiazzi, che porta non ce l'hanno.
        private static Cell? SullUscio(VillageMap mappa, PlaceRect luogo)
        {
            for (var y = luogo.Y - 1; y <= luogo.Y + luogo.H; y++)
            {
                for (var x = luogo.X - 1; x <= luogo.X + luogo.W; x++)
                {
                    if (x < 1 || y < 1 || x >= mappa.Width - 1 || y >= mappa.Height - 1
                        || mappa.Rows[y][x] != '+')
                    {
                        continue;
                    }
                    // Il lato di fuori: dei due vicini calpestabili della soglia,
                    // quello che non sta dentro il rettangolo.
                    foreach (var passo in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        var ax = x + passo.Item1;
                        var ay = y + passo.Item2;
                        var fuori = ax < luogo.X || ax >= luogo.X + luogo.W
                                    || ay < luogo.Y || ay >= luogo.Y + luogo.H;
                        if (fuori && (mappa.Rows[ay][ax] == '.' || mappa.Rows[ay][ax] == ','))
                        {
                            return new Cell(ax, ay);
                        }
                    }
                }
            }
            return null;
        }

        /// Le stanze: una luce accesa e quattro mobili contro le pareti.
        ///
        /// E' quello che mancava perche' una casa fosse una casa e non una scatola
        /// col tetto. La luce viene prima dei mobili: da quando c'e' un tetto,
        /// dentro non entra piu' niente, e una stanza buia con dentro una persona
        /// e' peggio di una stanza vuota.
        ///
        /// I mobili stanno contro il muro, mai in mezzo, mai sulla soglia e mai
        /// addosso a chi ci abita: il centro della stanza e' dove si cammina e
        /// dove si parla, e va lasciato libero. E' l'errore che ho appena fatto
        /// con le insegne.
        private static void Stanze(VillageMap mappa, float cella, Transform radice)
        {
            var dentro = new GameObject("stanze").transform;
            dentro.SetParent(radice);
            var occupate = new HashSet<int>();
            foreach (var pair in mappa.Spawns)
            {
                occupate.Add(pair.Value.Y * mappa.Width + pair.Value.X);
            }

            foreach (var pair in mappa.Places)
            {
                var luogo = pair.Value;
                if (!HaUnInterno(mappa, luogo) || DentroUnAltro(mappa, pair.Key, luogo))
                {
                    continue;
                }

                var centro = mappa.CenterOf(pair.Key);
                if (centro is { } fuoco)
                {
                    var lampada = new GameObject("luce " + pair.Key).AddComponent<Light>();
                    lampada.transform.SetParent(dentro);
                    lampada.transform.position =
                        new Vector3(fuoco.X * cella, AltezzaMuro - 0.55f, -fuoco.Y * cella);
                    lampada.type = LightType.Point;
                    // Una lampadina del 1987, non un faretto: gialla e bassa.
                    lampada.color = new Color(1f, 0.86f, 0.66f);
                    lampada.intensity = 1.35f;
                    lampada.range = Mathf.Max(luogo.W, luogo.H) * cella * 1.4f;
                    lampada.shadows = LightShadows.None;
                }

                var roba = Mobilia.TryGetValue(pair.Key, out var suoi) ? suoi : Casa;
                for (var y = luogo.Y; y < luogo.Y + luogo.H && y < mappa.Height - 1; y++)
                {
                    for (var x = luogo.X; x < luogo.X + luogo.W && x < mappa.Width - 1; x++)
                    {
                        if (mappa.Rows[y][x] != '~' || Caso(x, y, 109) < 0.62f
                            || occupate.Contains(y * mappa.Width + x)
                            || AccantoAUnaPorta(mappa, x, y))
                        {
                            continue;
                        }
                        if (!ControIlMuro(mappa, x, y, out var verso))
                        {
                            continue;
                        }
                        var mobile = Modello("casa", Scegli(roba, x, y, 113), dentro, x, y, cella);
                        if (mobile != null)
                        {
                            mobile.transform.position += new Vector3(verso.x, 0f, verso.y) * cella * 0.3f;
                        }
                    }
                }
            }
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

        /// I tetti.
        ///
        /// E' la cosa che trasforma una scatola di cubi in una casa, e non si puo'
        /// fare con gli edifici comprati: ogni luogo di questa mappa e' una stanza
        /// in cui si entra — dentro la bottega c'e' Matteo — e un edificio chiuso
        /// appoggiato sopra sigillerebbe la stanza col personaggio dentro. Il
        /// tetto invece sta sopra i muri che ci sono gia', a due falde, con la
        /// gronda che sporge: dall'esterno e' una casa, dall'interno non e'
        /// cambiato niente.
        private static void Tetti(VillageMap mappa, float cella, Transform radice)
        {
            var falda = new Falda();
            foreach (var pair in mappa.Places)
            {
                var luogo = pair.Value;
                if (!HaUnInterno(mappa, luogo) || DentroUnAltro(mappa, pair.Key, luogo))
                {
                    continue;
                }
                Tetto(falda, luogo, cella);
            }

            var tetti = new GameObject("tetti");
            tetti.transform.SetParent(radice);
            var mesh = falda.Mesh();
            tetti.AddComponent<MeshFilter>().sharedMesh = mesh;
            tetti.AddComponent<MeshRenderer>().sharedMaterial = Materiale(Lose);
        }

        private static bool HaUnInterno(VillageMap mappa, PlaceRect luogo)
        {
            for (var y = luogo.Y; y < luogo.Y + luogo.H && y < mappa.Height; y++)
            {
                for (var x = luogo.X; x < luogo.X + luogo.W && x < mappa.Width; x++)
                {
                    if (mappa.Rows[y][x] == '~')
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// Il magazzino e il seminterrato stanno dentro il deposito: sono tre
        /// nomi e un edificio solo, e tre tetti sovrapposti si vedono.
        private static bool DentroUnAltro(VillageMap mappa, string nome, PlaceRect luogo)
        {
            foreach (var pair in mappa.Places)
            {
                var altro = pair.Value;
                if (pair.Key == nome || altro.W * altro.H <= luogo.W * luogo.H)
                {
                    continue;
                }
                if (luogo.X >= altro.X && luogo.Y >= altro.Y
                    && luogo.X + luogo.W <= altro.X + altro.W
                    && luogo.Y + luogo.H <= altro.Y + altro.H)
                {
                    return true;
                }
            }
            return false;
        }

        private static void Tetto(Falda falda, PlaceRect luogo, float cella)
        {
            const float gronda = 0.45f;
            const float colmo = 1.3f;
            var x0 = (luogo.X - 0.5f - gronda) * cella;
            var x1 = (luogo.X + luogo.W - 0.5f + gronda) * cella;
            var z0 = -(luogo.Y - 0.5f - gronda) * cella;
            var z1 = -(luogo.Y + luogo.H - 0.5f + gronda) * cella;
            var basso = AltezzaMuro - 0.1f;
            var alto = AltezzaMuro + colmo;

            if (luogo.W >= luogo.H)
            {
                // Il colmo corre per il lungo, che e' come si copre una casa
                // stretta: l'acqua deve scendere dal lato corto.
                var zc = (z0 + z1) * 0.5f;
                falda.Quadrilatero(
                    new Vector3(x0, basso, z0), new Vector3(x1, basso, z0),
                    new Vector3(x1, alto, zc), new Vector3(x0, alto, zc));
                falda.Quadrilatero(
                    new Vector3(x0, alto, zc), new Vector3(x1, alto, zc),
                    new Vector3(x1, basso, z1), new Vector3(x0, basso, z1));
                falda.Triangolo(
                    new Vector3(x0, basso, z0), new Vector3(x0, alto, zc), new Vector3(x0, basso, z1));
                falda.Triangolo(
                    new Vector3(x1, basso, z0), new Vector3(x1, alto, zc), new Vector3(x1, basso, z1));
            }
            else
            {
                var xc = (x0 + x1) * 0.5f;
                falda.Quadrilatero(
                    new Vector3(x0, basso, z0), new Vector3(xc, alto, z0),
                    new Vector3(xc, alto, z1), new Vector3(x0, basso, z1));
                falda.Quadrilatero(
                    new Vector3(xc, alto, z0), new Vector3(x1, basso, z0),
                    new Vector3(x1, basso, z1), new Vector3(xc, alto, z1));
                falda.Triangolo(
                    new Vector3(x0, basso, z0), new Vector3(xc, alto, z0), new Vector3(x1, basso, z0));
                falda.Triangolo(
                    new Vector3(x0, basso, z1), new Vector3(xc, alto, z1), new Vector3(x1, basso, z1));
            }
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

            /// Quattro punti in ordine di giro. Ogni faccia si emette anche
            /// rovesciata: senza vedere il risultato non si indovina da che parte
            /// guarda una falda, e un tetto invisibile e' peggio di un tetto
            /// illuminato male.
            public void Quadrilatero(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var i = _punti.Count;
                _punti.Add(a); _punti.Add(b); _punti.Add(c); _punti.Add(d);
                _triangoli.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
                _punti.Add(a); _punti.Add(b); _punti.Add(c); _punti.Add(d);
                _triangoli.AddRange(new[] { i + 6, i + 5, i + 4, i + 7, i + 6, i + 4 });
            }

            public void Triangolo(Vector3 a, Vector3 b, Vector3 c)
            {
                var i = _punti.Count;
                _punti.Add(a); _punti.Add(b); _punti.Add(c);
                _triangoli.AddRange(new[] { i, i + 1, i + 2 });
                _punti.Add(a); _punti.Add(b); _punti.Add(c);
                _triangoli.AddRange(new[] { i + 5, i + 4, i + 3 });
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
