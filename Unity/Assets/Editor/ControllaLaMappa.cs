using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amnesia.Core;
using Amnesia.World;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// La mappa si puo' scrivere a mano, ed e' giusto cosi': e' un disegno fatto
    /// di caratteri, e si cambia meglio in un editor di testo che in una
    /// finestra. Quello che serve non e' un pennello — e' qualcuno che rilegga.
    ///
    /// Un paese scritto a mano si rompe in silenzio: una riga di 63 caratteri
    /// invece di 64, una porta che non da' sulla strada, un pezzo di paese
    /// staccato dal resto, una casa vuota. Niente di questo solleva un errore.
    /// Si scopre camminando, mezz'ora dopo, e non si capisce cosa e' successo.
    public static class ControllaLaMappa
    {
        [MenuItem("Amnesia/Controlla la mappa")]
        public static void Controlla()
        {
            var percorso = Percorso("village_map.json");
            if (!File.Exists(percorso))
            {
                Debug.LogError($"la mappa non si trova: {percorso}");
                return;
            }

            var caricata = VillageMap.Load(percorso);
            if (!caricata.IsOk)
            {
                Debug.LogError($"la mappa non si carica: {caricata.Message}");
                return;
            }
            var mappa = caricata.Value;
            var guai = new List<string>();

            for (var y = 0; y < mappa.Rows.Count; y++)
            {
                if (mappa.Rows[y].Length != mappa.Width)
                {
                    guai.Add($"riga {y}: e' lunga {mappa.Rows[y].Length} invece di {mappa.Width}");
                }
                foreach (var segno in mappa.Rows[y])
                {
                    if (!mappa.Legend.ContainsKey(segno))
                    {
                        guai.Add($"riga {y}: il carattere '{segno}' non sta in legenda");
                        break;
                    }
                }
            }
            if (mappa.Rows.Count != mappa.Height)
            {
                guai.Add($"il file ha {mappa.Rows.Count} righe invece di {mappa.Height}");
            }

            var partenza = mappa.Spawn("player") ?? mappa.CenterOf("casa_lipari");
            if (partenza is null)
            {
                guai.Add("il giocatore non ha un posto in cui nascere");
            }
            else
            {
                var raggiunte = Raggiungibili(mappa, partenza.Value);
                var calpestabili = 0;
                for (var y = 0; y < mappa.Height; y++)
                {
                    for (var x = 0; x < mappa.Width; x++)
                    {
                        if (mappa.IsWalkable(new Cell(x, y)))
                        {
                            calpestabili++;
                        }
                    }
                }
                if (raggiunte.Count != calpestabili)
                {
                    guai.Add($"il paese e' in piu' pezzi: da casa se ne raggiungono {raggiunte.Count} su {calpestabili}");
                }

                foreach (var chi in mappa.Spawns)
                {
                    if (!raggiunte.Contains(chi.Value))
                    {
                        guai.Add($"{chi.Key} nasce in ({chi.Value.X},{chi.Value.Y}), dove non si arriva a piedi");
                    }
                }

                foreach (var luogo in mappa.Places)
                {
                    var dentro = false;
                    for (var y = luogo.Value.Y; y < luogo.Value.Y + luogo.Value.H && !dentro; y++)
                    {
                        for (var x = luogo.Value.X; x < luogo.Value.X + luogo.Value.W && !dentro; x++)
                        {
                            dentro = raggiunte.Contains(new Cell(x, y));
                        }
                    }
                    // La galleria e' murata apposta: e' l'unico posto del paese
                    // che si nomina e non si apre.
                    if (!dentro && luogo.Key != "galleria")
                    {
                        guai.Add($"{luogo.Key} non si raggiunge a piedi");
                    }
                }
            }

            if (guai.Count == 0)
            {
                Debug.Log($"La mappa va bene: {mappa.Width}x{mappa.Height}, {mappa.Places.Count} luoghi, tutti raggiungibili.");
                return;
            }
            var referto = new StringBuilder($"La mappa ha {guai.Count} problemi:\n");
            foreach (var guaio in guai)
            {
                referto.Append("  · ").Append(guaio).Append('\n');
            }
            Debug.LogError(referto.ToString());
        }

        /// La legenda, stampata a comando: serve ogni volta che si apre il file
        /// e non ci si ricorda se la strada e' il punto o la virgola.
        [MenuItem("Amnesia/Spiega la legenda della mappa")]
        public static void Legenda()
        {
            Debug.Log(
                "village_map.json — una riga per riga del paese, un carattere per cella.\n" +
                "  ,  strada (ci si cammina)      .  terreno aperto (ci si cammina)\n" +
                "  ~  pavimento di una stanza     +  porta\n" +
                "  \"  sottobosco                  #  muro      T  albero\n" +
                "  ^  roccia                      =  acqua o ferrovia\n\n" +
                "Le righe devono essere tutte lunghe `width`, e i rettangoli in «places»\n" +
                "dicono dove stanno i luoghi con un nome. Dopo ogni modifica: Amnesia →\n" +
                "Controlla la mappa.\n\n" +
                "ATTENZIONE: `tools/paese.py` riscrive questo file da capo. Se lo modifichi\n" +
                "a mano, quel comando non va piu' lanciato — o si sceglie di cambiare il\n" +
                "generatore invece del file.");
        }

        private static string Percorso(string nome)
        {
            var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content", nome));
            return File.Exists(repository) ? repository : Path.Combine(Application.streamingAssetsPath, nome);
        }

        private static HashSet<Cell> Raggiungibili(VillageMap mappa, Cell partenza)
        {
            var viste = new HashSet<Cell> { partenza };
            var coda = new Queue<Cell>();
            coda.Enqueue(partenza);
            while (coda.Count > 0)
            {
                var cella = coda.Dequeue();
                foreach (var passo in new[]
                         {
                             new Cell(cella.X + 1, cella.Y), new Cell(cella.X - 1, cella.Y),
                             new Cell(cella.X, cella.Y + 1), new Cell(cella.X, cella.Y - 1),
                         })
                {
                    if (!viste.Contains(passo) && mappa.IsWalkable(passo))
                    {
                        viste.Add(passo);
                        coda.Enqueue(passo);
                    }
                }
            }
            return viste;
        }
    }
}
