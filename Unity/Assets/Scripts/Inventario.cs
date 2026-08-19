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
        /// Cosa hai addosso, e con quale nome si mostra. Il nome esatto conta:
        /// il motore accetta l'id, la dicitura e l'etichetta breve, ma non
        /// un sinonimo inventato dal giocatore, e non deve toccare a lui
        /// indovinarlo.
        public static string Oggetti(Bootstrap gioco)
        {
            var scritto = new StringBuilder();
            var tasche = gioco.Taccuino.Tasche();
            if (tasche.Count == 0)
            {
                return "Non hai niente in tasca.";
            }
            foreach (var oggetto in tasche)
            {
                var nome = string.IsNullOrEmpty(oggetto.Name) ? oggetto.Id : oggetto.Name;
                scritto.Append("· ").Append(nome).Append('\n');
                if (!string.IsNullOrEmpty(oggetto.Description))
                {
                    scritto.Append("   ").Append(oggetto.Description).Append('\n');
                }
            }
            var primo = tasche[0];
            scritto.Append("\nSi mostra scrivendo  [mostra: ")
                .Append(string.IsNullOrEmpty(primo.Name) ? primo.Id : primo.Name)
                .Append("]  dentro una frase.");
            return scritto.ToString();
        }

        /// Cosa ti hanno detto, attribuito e senza giudizio. Il taccuino non
        /// segna mai niente come vero: sette righe su trentotto sono false,
        /// dette da gente che ci crede, e distinguerle e' la partita.
        public static string Righe(Bootstrap gioco)
        {
            var righe = gioco.Taccuino.Dette();
            if (righe.Count == 0)
            {
                return "Il taccuino e' ancora bianco.\n\nCi finisce quello che la gente ti dice, con il nome di chi l'ha detto.";
            }
            var scritto = new StringBuilder();
            foreach (var detta in righe)
            {
                scritto.Append("· «").Append(detta.Testo).Append("»\n   ");
                scritto.Append(string.Join(", ", detta.Bocche.Select(gioco.NomeDi)));
                if (detta.Volte > detta.Bocche.Count)
                {
                    scritto.Append("  ·  ").Append(detta.Volte).Append(" volte");
                }
                scritto.Append('\n');
            }
            if (righe.Count > 1)
            {
                scritto.Append("\nDue righe si accostano scrivendo  [confronto: ")
                    .Append(righe[0].Id).Append(" | ").Append(righe[1].Id).Append("]");
            }
            return scritto.ToString();
        }

    }
}
