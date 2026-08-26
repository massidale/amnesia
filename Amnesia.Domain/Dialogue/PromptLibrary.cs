namespace Amnesia.Dialogue;

/// Carica le schede dei personaggi da una cartella di prompt.
///
/// Una scheda per gradino sta in una sottocartella — "matteo/M2.md" diventa la
/// chiave "matteo/M2"; una scheda unica sta al primo livello — "anna.md" diventa
/// "anna". Le regole comuni ("rules.md") non sono una scheda e restano fuori.
///
/// Vive nel dominio, non nell'app: Bootstrap, la sonda e i test caricano tutti di
/// qui, cosi' la logica e' una sola e testata una volta, e le sottocartelle si
/// leggono ovunque senza ripetere la ricorsione in tre posti.
public static class PromptLibrary
{
    public const string RulesId = "rules";

    public static Dictionary<string, string> Load(string promptsDir)
    {
        var schede = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(promptsDir, "*.md", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(promptsDir, file);
            var id = rel.Substring(0, rel.Length - ".md".Length).Replace('\\', '/');
            if (id != RulesId)
            {
                schede[id] = File.ReadAllText(file);
            }
        }
        return schede;
    }

    /// La persona a cui appartiene una chiave di scheda: "matteo/M2" e "matteo/M0"
    /// sono lo stesso Matteo; "anna" e' Anna.
    public static string PersonaDi(string schedaId)
    {
        var barra = schedaId.IndexOf('/');
        return barra < 0 ? schedaId : schedaId.Substring(0, barra);
    }
}
