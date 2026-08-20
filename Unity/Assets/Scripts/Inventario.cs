using System.Linq;
using System.Text;

namespace AmnesiaUnity
{
    /// Le tasche e il taccuino, in parole. Li leggono in due — il menu di
    /// pausa e i comandi dentro la conversazione — e per questo il testo si
    /// scrive qui una volta sola: due punti in cui scrivere lo stesso elenco
    /// sono due punti in cui diverge.
    public static class Inventario
    {
        /// Quello che non sta in tasca: sta scritto. La frase e il nome sono le
        /// prime due righe del taccuino — gliele ha dettate sua madre al
        /// capezzale — e vanno lette, non elencate fra gli oggetti: sono l'unica
        /// cosa che Giorgio si porta dietro da ventun anni.
        private static readonly string[] PrimaPagina = { "frase", "taccuino" };

        /// La prima pagina, scritta all'ospedale. Sono due righe, e sono tutto
        /// il gioco: cinque parole che non vogliono dire niente e un nome di
        /// donna. Nessuna delle due, per Giorgio, significa ancora qualcosa.
        private static string PrimeDueRighe(Bootstrap gioco)
        {
            var scritto = new StringBuilder("PRIMA PAGINA — scritta all'ospedale, sotto dettatura di mia madre\n");
            var frase = gioco.Items.Find("frase");
            if (frase != null)
            {
                scritto.Append(frase.Visible).Append('\n');
            }
            scritto.Append("Elena. Ripetevo anche questo nome, da bambino. Mia madre non l'ha mai collegato a niente.\n\n");
            return scritto.ToString();
        }
        /// Cosa hai addosso, e con quale nome si mostra. Il nome esatto conta:
        /// il motore accetta l'id, la dicitura e l'etichetta breve, ma non
        /// un sinonimo inventato dal giocatore, e non deve toccare a lui
        /// indovinarlo.
        public static string Oggetti(Bootstrap gioco)
        {
            var scritto = new StringBuilder();
            var tasche = gioco.Taccuino.Tasche()
                .Where(oggetto => System.Array.IndexOf(PrimaPagina, oggetto.Id) < 0).ToList();
            if (tasche.Count == 0)
            {
                return "Non hai niente in tasca.";
            }
            foreach (var oggetto in tasche)
            {
                var nome = string.IsNullOrEmpty(oggetto.Name) ? oggetto.Id : oggetto.Name;
                scritto.Append(nome.ToUpperInvariant()).Append('\n');
                // Quello che si vede guardandolo, per esteso: un oggetto che il
                // giocatore non puo' leggere e' un oggetto che non ha.
                if (!string.IsNullOrEmpty(oggetto.Visible))
                {
                    scritto.Append(oggetto.Visible).Append('\n');
                }
                if (!string.IsNullOrEmpty(oggetto.Description))
                {
                    scritto.Append("— ").Append(oggetto.Description).Append('\n');
                }
                scritto.Append('\n');
            }
            var primo = tasche[0];
            scritto.Append("Si mostra scrivendo  [mostra: ")
                .Append(string.IsNullOrEmpty(primo.Name) ? primo.Id : primo.Name)
                .Append("]  dentro una frase.");
            return scritto.ToString();
        }

/// Il taccuino: le cose considerate vere fino a questo momento, e quelle
        /// che la storia ha smentito — cancellate a penna, non strappate. E' la
        /// bussola del giocatore, non un archivio.
        public static string Righe(Bootstrap gioco)
        {
            var scritto = new StringBuilder(PrimeDueRighe(gioco));
            var righe = gioco.Taccuino.Convinzioni();
            if (righe.Count == 0)
            {
                return scritto.Append("Il resto del taccuino e' bianco.").ToString();
            }
            foreach (var riga in righe)
            {
                if (riga.Cancellata)
                {
                    // Niente barrato nei font di UI Text: la penna si rende con
                    // il colore spento e la parola "cancellato".
                    scritto.Append("<color=#6E6656>· ").Append(riga.Testo)
                        .Append("   — cancellato</color>\n");
                }
                else
                {
                    scritto.Append("· ").Append(riga.Testo).Append('\n');
                }
            }
            return scritto.ToString().TrimEnd('\n');
        }

    }
}
