using Amnesia.Core;
using Amnesia.Dialogue;

namespace Amnesia.Game;

/// Chi apre bocca per primo. Il giocatore entra in bottega e il falegname alza
/// la testa: e' una battuta autoriale, non una chiamata al modello — costa
/// zero, non puo' sbagliare, e dice in che rapporto siete.
///
/// Si dice una volta per gradino. La seconda volta che entri lo stesso giorno
/// non ti risaluta nessuno; quando invece sei salito di un gradino, la prima
/// cosa che senti e' che qualcosa fra voi e' cambiato.
public sealed class Greeter
{
    private readonly GreetingTable _saluti;
    private readonly PositionTable _posizioni;
    private readonly ItemCatalog? _items;

    public Greeter(GreetingTable saluti, PositionTable posizioni, ItemCatalog? items = null)
    {
        _saluti = saluti;
        _posizioni = posizioni;
        _items = items;
    }

    /// Mondo e registro si passano a ogni chiamata: la sessione sostituisce il
    /// mondo a ogni turno riuscito, e un saluto che scrivesse su una copia
    /// vecchia si ripeterebbe per sempre.
    public string Apri(WorldState world, ConversationLog log, string npcId)
    {
        var posizione = _posizioni.HasLadder(npcId)
            ? _posizioni.PositionOf(npcId, world)
            : GreetingTable.Sempre;
        var chiave = $"salutato:{npcId}:{posizione}";
        if (world.Flags.TryGetValue(chiave, out var gia) && gia)
        {
            return "";
        }
        var riga = _saluti.Line(npcId, posizione);
        if (riga.Length == 0)
        {
            return "";
        }
        world.Flags[chiave] = true;
        var ricevuti = new List<string>();
        if (npcId == "rosa")
        {
            var prima = new HashSet<string>(world.ItemOwners.Keys);
            ConsegneNarrative.PrimoIncontroRosa(world);
            ricevuti.AddRange(world.ItemOwners.Keys.Where(id => !prima.Contains(id)));
        }
        // Va nel registro come una battuta qualsiasi: se il personaggio ha
        // detto «ti trovo bene», il modello deve averlo davanti al turno dopo,
        // o si contraddice al primo scambio.
        log.Append(npcId, ChatRole.Assistant, riga, ConsegneNarrative.Didascalia(ricevuti, _items));
        return riga;
    }
}
