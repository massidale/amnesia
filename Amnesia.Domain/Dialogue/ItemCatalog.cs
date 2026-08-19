namespace Amnesia.Dialogue;

/// Le tre grafie di un oggetto. `Visible` e' cio' che un uomo vede sul tavolo ed
/// e' l'unica che entra in un prompt; `Name` e' l'etichetta corta che l'inventario
/// stampa e `Description` e' scritta per il giocatore alla sua scrivania. Le
/// ultime due non sono percepibili dentro la finzione: se una delle due colasse
/// nella coda dinamica, ogni comportamento gia' misurato sarebbe stato di
/// nascosto rivalidato su prompt diversi da quelli su cui e' stato misurato.
public sealed record ItemDefinition(string Id, string Visible, string Name = "", string Description = "");

/// Il catalogo degli oggetti: contenuto d'autore, non stato di partita. Il mondo
/// salva solo chi possiede cosa, perche' come si chiama una cosa non cambia mai
/// e non ha ragione di farsi un giro sul disco a ogni salvataggio.
public sealed class ItemCatalog
{
    private readonly List<ItemDefinition> _items;
    private readonly Dictionary<string, ItemDefinition> _byId;

    public ItemCatalog(params ItemDefinition[] items)
        : this((IEnumerable<ItemDefinition>)items)
    {
    }

    public ItemCatalog(IEnumerable<ItemDefinition> items)
    {
        _items = items.ToList();
        _byId = _items.ToDictionary(item => item.Id);
    }

    public IReadOnlyList<ItemDefinition> Items => _items;

    public ItemDefinition? Find(string itemId) =>
        _byId.TryGetValue(itemId, out var item) ? item : null;

    /// Cio' che il personaggio vede. Un oggetto che il catalogo non conosce vale
    /// il proprio id: il prompt dice una cosa goffa invece di dire il vuoto, e chi
    /// legge la trascrizione capisce subito quale dato manca.
    public string VisibleOf(string itemId)
    {
        var item = Find(itemId);
        return item is null || item.Visible.Length == 0 ? itemId : item.Visible;
    }
}
