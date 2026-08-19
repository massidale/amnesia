using System.Text.RegularExpressions;
using Amnesia.Core;

namespace Amnesia.Dialogue;

/// Cio' che il giocatore ha detto davvero, una volta che il motore ha letto i
/// tag: le parole che arrivano al modello, gli oggetti che ha davvero mostrato e
/// i tag rifiutati, che il gioco gli ridice invece di far finta di niente.
public sealed record PlayerUtterance(
    string Spoken,
    IReadOnlyList<string> ShownItemIds,
    IReadOnlyList<string> InvalidTags);

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

    public static PlayerUtterance Parse(string raw, WorldState world, ItemCatalog items, string playerId)
    {
        var shown = new List<string>();
        var invalid = new List<string>();
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
            if (spoken == before)
            {
                break;
            }
        }
        spoken = spoken.Replace("  ", " ").Replace(" ,", ",").Replace(" .", ".").Trim();
        return new PlayerUtterance(spoken, shown, invalid);
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
