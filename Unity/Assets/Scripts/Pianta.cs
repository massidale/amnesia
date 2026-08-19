using System.Collections.Generic;
using Amnesia.World;
using UnityEngine;

namespace AmnesiaUnity
{
    /// La pianta di San Rocco, disegnata dagli stessi caratteri con cui il paese
    /// e' costruito: se un giorno si sposta un muro nel file, si sposta qui.
    ///
    /// Non e' una concessione al giocatore: Giorgio e' nato qui e il paese lo sa
    /// a memoria — l'unica cosa che non sa e' cosa ci sia successo. Percio' la
    /// pianta mostra i posti, e non mostra mai dove sono le persone: quello si
    /// scopre bussando, che e' tutto il gioco.
    public static class Pianta
    {
        /// I nomi come li direbbe uno del posto. Stanno qui e non nei dati
        /// perche' sono didascalie, non contenuto.
        public static readonly Dictionary<string, string> Nomi = new Dictionary<string, string>
        {
            ["cava"] = "la cava", ["galleria"] = "la galleria murata", ["castagneto"] = "il castagneto",
            ["bottega"] = "la bottega di Matteo", ["segheria"] = "la segheria", ["casa_lipari"] = "casa mia",
            ["casa_valli"] = "casa Valli", ["casa_ferro"] = "casa Ferro", ["strada"] = "la strada",
            ["chiesa"] = "la chiesa", ["canonica"] = "la canonica", ["piazza"] = "la piazza",
            ["bar"] = "il bar", ["negozio"] = "il negozio",
            ["panetteria"] = "il forno", ["giardino"] = "il giardino", ["stazione"] = "la stazione", ["deposito"] = "il deposito",
            ["magazzino_b17"] = "il magazzino B-17",
        };

        public static string NomeDi(string luogo) =>
            Nomi.TryGetValue(luogo, out var nome) ? nome : luogo.Replace('_', ' ');

        public static bool SiScrive(string luogo) => NomeDi(luogo).Length > 0;

        public static Texture2D Disegna(VillageMap mappa)
        {
            var pianta = new Texture2D(mappa.Width, mappa.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
            };
            for (var y = 0; y < mappa.Height; y++)
            {
                for (var x = 0; x < mappa.Width; x++)
                {
                    // La texture cresce dal basso, la mappa dall'alto.
                    pianta.SetPixel(x, mappa.Height - 1 - y, Scenografia.ColoreDi(mappa.Rows[y][x]));
                }
            }
            pianta.Apply();
            return pianta;
        }
    }
}
