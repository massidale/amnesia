using Amnesia.Core;
using Amnesia.Dialogue;

namespace Amnesia.Game;

/// Una riga del taccuino di Giorgio: quello che crede vero adesso, oppure una
/// cosa che credeva e che la storia ha smentito — cancellata a penna, non
/// strappata: i segni degli errori sono il modo in cui si vede la trama
/// muoversi.
public sealed record RigaDelTaccuino(string Id, string Testo, bool Cancellata);

/// Il taccuino di Giorgio. Segna le cose rilevanti considerate vere fino a
/// questo momento, nell'ordine scritto dall'autore: e' la bussola con cui il
/// giocatore va avanti nella trama.
///
/// Una riga compare quando una qualunque delle sue dichiarazioni e' stata
/// detta davvero — registrata dal motore con lo strumento, mai dedotta dalla
/// prosa — e viene cancellata quando compare la riga che la smentisce.
public sealed class Taccuino
{
    private readonly WorldState _world;
    private readonly ConvinzioniTable _convinzioni;
    private readonly ItemCatalog _oggetti;
    private readonly string _giocatore;

    public Taccuino(WorldState world, ConvinzioniTable convinzioni, ItemCatalog oggetti, string giocatore = "player")
    {
        _world = world;
        _convinzioni = convinzioni;
        _oggetti = oggetti;
        _giocatore = giocatore;
    }

    /// Cosa si ha in tasca, con i nomi con cui si puo' mostrare. L'ordine e'
    /// quello del catalogo e non quello del dizionario dei proprietari: una
    /// tasca che si riordina da sola a ogni apertura non e' una tasca.
    public IReadOnlyList<ItemDefinition> Tasche() =>
        _oggetti.Items
            .Where(oggetto => _world.ItemOwners.TryGetValue(oggetto.Id, out var chi) && chi == _giocatore)
            .ToList();

    /// Le righe del taccuino, comprese quelle cancellate: si mostrano barrate,
    /// perche' vedere cosa si credeva ieri e' meta' del capire cosa e' vero oggi.
    public IReadOnlyList<RigaDelTaccuino> Convinzioni()
    {
        var register = new Register(_world);
        bool Comparsa(Convinzione riga) =>
            riga.Iniziale || riga.Quando.Any(id => register.SupportsFor(id).Count > 0);

        var comparse = _convinzioni.Righe.Where(Comparsa).ToList();
        var smentite = comparse
            .Where(riga => riga.Sostituisce.Length > 0)
            .Select(riga => riga.Sostituisce)
            .ToHashSet();
        return comparse
            .Select(riga => new RigaDelTaccuino(riga.Id, riga.Testo, smentite.Contains(riga.Id)))
            .ToList();
    }
}
