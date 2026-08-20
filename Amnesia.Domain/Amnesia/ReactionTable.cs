using System.Text.Json;
using Amnesia.Core;

namespace Amnesia;

/// Il copione delle reazioni agli oggetti: per personaggio, per gradino, per
/// oggetto, COSA FA questa persona quando quella cosa le finisce sul banco.
///
/// Esiste perche' la demo ha dimostrato che la prosa sperata non basta: ad Anna
/// si mostrava la chiave e lei non diceva del magazzino finche' non si
/// insisteva, e un giocatore che non sa che l'informazione esiste ha davanti un
/// vicolo cieco. La reazione a un oggetto chiave non e' una sfumatura di
/// recitazione: e' un ingranaggio del gioco, e gli ingranaggi sono dati.
///
/// La ricerca scende: prima il gradino corrente, poi i gradini gia' saliti
/// dall'alto in basso, poi la voce del personaggio senza gradino («sempre»),
/// poi il default per chiunque. Cosi' un gradino nuovo ridefinisce solo cio'
/// che cambia, e un oggetto senza copione ha comunque una risposta: «questa
/// cosa non la conosci, dillo».
public sealed class ReactionTable
{
    public const string DefaultPath = "content/amnesia/reazioni.json";

    /// La chiave della tabella valida per ogni personaggio.
    public const string Chiunque = "chiunque";

    /// Il gradino dei personaggi che non hanno gradini.
    public const string Sempre = "sempre";

    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _reazioni;

    private ReactionTable(Dictionary<string, Dictionary<string, Dictionary<string, string>>> reazioni) =>
        _reazioni = reazioni;

    public static ReactionTable Empty() => new(new());

    public static Result<ReactionTable> Load(string path)
    {
        if (!File.Exists(path))
        {
            return Result<ReactionTable>.Fail("reactions_not_found", $"copione delle reazioni non leggibile: {path}");
        }
        return FromJson(File.ReadAllText(path));
    }

    public static Result<ReactionTable> FromJson(string json)
    {
        Dto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<Dto>(json);
        }
        catch (JsonException)
        {
            return Result<ReactionTable>.Fail("invalid_reactions", "copione delle reazioni illeggibile");
        }
        if (dto?.Reazioni is null)
        {
            return Result<ReactionTable>.Fail("invalid_reactions", "copione delle reazioni illeggibile");
        }
        return Result<ReactionTable>.Ok(new ReactionTable(dto.Reazioni));
    }

    /// La reazione di questo personaggio a questo oggetto, dato l'elenco dei
    /// gradini raggiunti (dal primo all'ultimo; vuoto per chi non ha scala).
    /// Stringa vuota se nessuno ha scritto niente: il chiamante decide il
    /// ripiego.
    public string Reazione(string npcId, IReadOnlyList<string> gradiniRaggiunti, string itemId)
    {
        if (_reazioni.TryGetValue(npcId, out var scala))
        {
            for (var i = gradiniRaggiunti.Count - 1; i >= 0; i--)
            {
                if (scala.TryGetValue(gradiniRaggiunti[i], out var gradino)
                    && gradino.TryGetValue(itemId, out var testo))
                {
                    return testo;
                }
            }
            if (scala.TryGetValue(Sempre, out var sempre) && sempre.TryGetValue(itemId, out var suo))
            {
                return suo;
            }
        }
        if (_reazioni.TryGetValue(Chiunque, out var tutti)
            && tutti.TryGetValue(Sempre, out var comune)
            && comune.TryGetValue(itemId, out var difetto))
        {
            return difetto;
        }
        return "";
    }

    private sealed class Dto
    {
        public Dictionary<string, Dictionary<string, Dictionary<string, string>>>? Reazioni { get; set; }
    }
}
