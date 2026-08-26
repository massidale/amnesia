using System.Text.Json;

namespace Amnesia.Dialogue;

/// I blocchi di conoscenza condivisa, in prosa: la versione del paese e — in
/// futuro — altri blocchi tematici (il Circolo, le ricerche di Andrea…). Ogni
/// personaggio ne riceve una permutazione, la sua conoscenza di base, che si
/// somma alla scheda: la scheda decide COME dire le cose, i blocchi COSA sa.
///
/// Sostituiscono il vecchio blocco <posizione>, che infilava i testi delle
/// dichiarazioni uno dietro l'altro e confondeva le acque. Qui la conoscenza e'
/// prosa coesa, e chi non ha un assegnamento non riceve niente: la sua sta tutta
/// nella scheda (i guardinghi con la scala, Wanda ed Elena con la loro versione).
///
/// I blocchi stanno in "prompts/conoscenze/<id>.md"; l'assegnamento in
/// "prompts/conoscenze.json" — { "beppe": ["base"], "rosa": ["base"], ... }.
public sealed class ConoscenzeBase
{
    private readonly IReadOnlyDictionary<string, string> _blocchi;
    private readonly IReadOnlyDictionary<string, List<string>> _assegnati;

    private ConoscenzeBase(
        IReadOnlyDictionary<string, string> blocchi,
        IReadOnlyDictionary<string, List<string>> assegnati)
    {
        _blocchi = blocchi;
        _assegnati = assegnati;
    }

    public static ConoscenzeBase Empty() =>
        new(new Dictionary<string, string>(), new Dictionary<string, List<string>>());

    /// La prosa che questo personaggio riceve: i blocchi assegnati, in ordine,
    /// concatenati. Vuota se non ne ha — allora sa solo quello che dice la scheda.
    public string PerPersonaggio(string npcId)
    {
        if (!_assegnati.TryGetValue(npcId, out var ids))
        {
            return "";
        }
        var pezzi = ids.Where(_blocchi.ContainsKey).Select(id => _blocchi[id]);
        return string.Join("\n\n", pezzi);
    }

    public static ConoscenzeBase Load(string promptsDir)
    {
        var blocchi = new Dictionary<string, string>();
        var dir = Path.Combine(promptsDir, "conoscenze");
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.md"))
            {
                blocchi[Path.GetFileNameWithoutExtension(file)] = File.ReadAllText(file);
            }
        }

        var assegnati = new Dictionary<string, List<string>>();
        var mappa = Path.Combine(promptsDir, "conoscenze.json");
        if (File.Exists(mappa))
        {
            assegnati = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(mappa))
                ?? new Dictionary<string, List<string>>();
        }

        return new ConoscenzeBase(blocchi, assegnati);
    }
}
