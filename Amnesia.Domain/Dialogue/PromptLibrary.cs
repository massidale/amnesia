namespace Amnesia.Dialogue;

/// Carica le schede dei personaggi da una cartella di prompt.
///
/// Una scheda per gradino sta in una sottocartella — "matteo/M2.md" diventa la
/// chiave "matteo/M2"; una scheda unica sta al primo livello — "anna.md" diventa
/// "anna". I file condivisi ("rules.md", "mondo.md", "conoscenze_base.md") non
/// sono schede di personaggio e restano fuori: li assembla il prefisso.
///
/// Vive nel dominio, non nell'app: Bootstrap, la sonda e i test caricano tutti di
/// qui, cosi' la logica e' una sola e testata una volta.
public static class PromptLibrary
{
    public const string RulesId = "rules";
    public const string MondoId = "mondo";
    public const string ConoscenzeDir = "conoscenze";

    public static Dictionary<string, string> Load(string promptsDir)
    {
        var schede = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(promptsDir, "*.md", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(promptsDir, file);
            var id = rel.Substring(0, rel.Length - ".md".Length).Replace('\\', '/');
            // Fuori: le regole, il mondo, e i blocchi di conoscenza condivisa
            // (prompts/conoscenze/*.md) — non sono schede di personaggio.
            if (id == RulesId || id == MondoId || PersonaDi(id) == ConoscenzeDir)
            {
                continue;
            }
            schede[id] = File.ReadAllText(file);
        }
        return schede;
    }

    /// Il prefisso condiviso da OGNI prompt: le regole di recitazione piu' il
    /// mondo (il paese, la gente, Giorgio). Universale, e sta in cache.
    public static string SharedPrefix(string promptsDir)
    {
        var regole = File.ReadAllText(Path.Combine(promptsDir, RulesId + ".md"));
        var mondo = Path.Combine(promptsDir, MondoId + ".md");
        return File.Exists(mondo)
            ? regole + "\n\n" + File.ReadAllText(mondo)
            : regole;
    }

    /// La persona a cui appartiene una chiave di scheda: "matteo/M2" e "matteo/M0"
    /// sono lo stesso Matteo; "anna" e' Anna.
    public static string PersonaDi(string schedaId)
    {
        var barra = schedaId.IndexOf('/');
        return barra < 0 ? schedaId : schedaId.Substring(0, barra);
    }
}
