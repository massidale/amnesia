using Amnesia.Core;
using Amnesia.Dialogue;

namespace Amnesia.Game;

/// Una riga del taccuino: cosa e' stato detto, e da quali bocche. Mai se sia
/// vero — la colonna della verita' esiste nei dati e non esce mai di li'.
public sealed record RigaDelTaccuino(string Id, string Testo, IReadOnlyList<string> Bocche, int Volte);

/// Il taccuino di Giorgio. Non e' una lista di missioni scritta da un autore:
/// e' il registro del motore mostrato com'e'. Quello che il gioco usa per
/// decidere e quello che il giocatore legge sono la stessa cosa, o si finisce a
/// discutere di una prova che il motore non ha mai visto.
///
/// Registra citazioni, mai conclusioni. Ci finisce la versione del paese, ci
/// finisce la scampagnata di Matteo, tutto con la stessa calligrafia e senza
/// asterischi: su trentotto dichiarazioni sette sono false, dette da gente che
/// ci crede, e distinguerle e' il gioco.
public sealed class Taccuino
{
    private readonly WorldState _world;
    private readonly DeclarationTable _tabella;
    private readonly ItemCatalog _oggetti;
    private readonly string _giocatore;

    public Taccuino(WorldState world, DeclarationTable tabella, ItemCatalog oggetti, string giocatore = "player")
    {
        _world = world;
        _tabella = tabella;
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

    /// Le dichiarazioni raccolte, nell'ordine in cui sono entrate.
    public IReadOnlyList<RigaDelTaccuino> Dette() =>
        _world.Declarations
            .OrderBy(riga => riga.Value.Sequence)
            .Select(riga => new RigaDelTaccuino(
                riga.Key,
                _tabella.TextOf(riga.Key),
                riga.Value.Supports.ToList(),
                riga.Value.Supports.Sum(bocca => riga.Value.Counts.TryGetValue(bocca, out var volte) ? volte : 0)))
            .ToList();
}
