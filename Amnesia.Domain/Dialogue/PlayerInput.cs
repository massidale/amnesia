using System.Text.RegularExpressions;
using Amnesia.Core;

namespace Amnesia.Dialogue;

/// Cio' che il giocatore ha detto davvero, una volta che il motore ha letto i
/// tag: le parole che arrivano al modello, gli oggetti che ha davvero mostrato e
/// i tag rifiutati, che il gioco gli ridice invece di far finta di niente.
public sealed record PlayerUtterance(
    string Spoken,
    IReadOnlyList<string> ShownItemIds,
    IReadOnlyList<string> InvalidTags,
    Confronto? Confronto = null);

/// Due righe del taccuino messe una accanto all'altra. Meccanicamente e' una
/// coppia di identificativi, verificabile, senza niente da interpretare — ma il
/// lavoro e' tutto del giocatore, perche' la partita sta nello scegliere quali
/// due. Un modello puo' negare un'accusa; non puo' disdire due righe che il
/// motore ha registrato.
public sealed record Confronto(string First, string Second);

/// Il confine di sicurezza del gioco: un modello puo' mentire a parole, ma non
/// puo' muovere la merce. Il tag `[mostra: <oggetto>]` lo legge il motore — mai
/// il modello — ed e' qui, e solo qui, che si decide cosa il giocatore possiede.
/// C'e' un canale solo: i vecchi tag di dono e di offerta sono stati tolti, e con
/// loro la superficie che si portavano dietro.
public static class PlayerInput
{
    // Insensibile alle maiuscole e tollerante allo spazio prima dei due punti:
    // ogni variante di sintassi va tolta, o il testo di un tag forgiato resta in
    // piedi e arriva al prompt.
    private static readonly Regex TagPattern =
        new(@"\[mostra\s*:\s*([^\]]+)\]", RegexOptions.IgnoreCase);

    // La stessa proprieta' del tag `mostra`, applicata alle parole: il giocatore
    // accosta due righe soltanto se le ha raccolte davvero.
    private static readonly Regex ConfrontoPattern =
        new(@"\[confronto\s*:\s*([^\]|]+)\|([^\]]+)\]", RegexOptions.IgnoreCase);

    public static PlayerUtterance Parse(string raw, WorldState world, ItemCatalog items, string playerId)
    {
        var shown = new List<string>();
        var invalid = new List<string>();
        var register = new Register(world);
        Confronto? confronto = null;
        var spoken = raw;
        // Ogni passata cerca sul testo gia' ripulito, mai sull'input originale: i
        // tag si annidano, e toglierne uno insieme ne mutila e ne espone altri.
        // Cercare su `raw` lascerebbe un tag mutilato in piedi dentro le parole del
        // giocatore — esattamente la falla per cui questa classe esiste.
        // Si spazza finche' il testo smette di cambiare; ogni passata lo accorcia,
        // quindi la spazzata finisce.
        while (true)
        {
            var before = spoken;
            foreach (Match tag in TagPattern.Matches(before))
            {
                var wanted = tag.Groups[1].Value.Trim();
                var itemId = Resolve(wanted, world, items, playerId);
                if (itemId.Length == 0)
                {
                    invalid.Add(wanted);
                }
                else
                {
                    shown.Add(itemId);
                }
                spoken = spoken.Replace(tag.Value, "");
            }
            // Nella STESSA spazzata dei tag `mostra`, e sul testo che quella
            // passata ha appena lasciato: un ciclo separato riaprirebbe esattamente
            // il buco che la spazzata esiste per chiudere, e un `confronto` che
            // inghiotte un `mostra` resterebbe in piedi come testo forgiabile.
            foreach (Match tag in ConfrontoPattern.Matches(spoken))
            {
                var first = tag.Groups[1].Value.Trim();
                var second = tag.Groups[2].Value.Trim();
                // Una riga mai raccolta non si puo' accostare, esattamente come non
                // si puo' mostrare un oggetto che non si possiede. E un turno porta
                // un accostamento solo: il secondo tag e' una mossa che questo turno
                // non puo' fare, e il gioco lo ridice invece di ingoiarla.
                if (confronto is null
                    && register.SupportsFor(first).Count > 0
                    && register.SupportsFor(second).Count > 0)
                {
                    confronto = new Confronto(first, second);
                }
                else
                {
                    invalid.Add(first);
                    invalid.Add(second);
                }
                spoken = spoken.Replace(tag.Value, "");
            }
            if (spoken == before)
            {
                break;
            }
        }
        spoken = spoken.Replace("  ", " ").Replace(" ,", ",").Replace(" .", ".").Trim();
        return new PlayerUtterance(spoken, shown, invalid, confronto);
    }

    /// Ogni nome con cui il giocatore puo' aver letto la cosa. L'id e' quello che
    /// l'inventario gli mette davanti; `Visible` e' quello che il personaggio dice
    /// ad alta voce; `Name` e' l'etichetta corta che l'inventario stampa di fianco
    /// all'id. Chi legge «Medaglione» e lo scrive non deve sentirsi rispondere che
    /// quella cosa non ce l'ha.
    private static string Resolve(string wanted, WorldState world, ItemCatalog items, string playerId)
    {
        var lowered = wanted.ToLowerInvariant();
        // Un tag che non nomina niente non corrisponde a niente. Senza questa
        // guardia un oggetto a cui manca `Name` — o `Visible` — risponderebbe a
        // `[mostra:  ]`, perche' "" == "" per ogni campo che non ha.
        if (lowered.Length == 0)
        {
            return "";
        }
        foreach (var item in items.Items)
        {
            if (!world.ItemOwners.TryGetValue(item.Id, out var owner) || owner != playerId)
            {
                continue;
            }
            foreach (var spelling in new[] { item.Id, item.Visible, item.Name })
            {
                if (spelling.ToLowerInvariant() == lowered)
                {
                    return item.Id;
                }
            }
        }
        return "";
    }

    /// Le parole del giocatore non possono fabbricare blocchi del motore: si
    /// neutralizza il markup.
    public static string Sanitize(string text) => text.Replace("<", "‹").Replace(">", "›");
}
