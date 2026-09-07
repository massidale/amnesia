using Amnesia.Core;
using Amnesia.Dialogue;

namespace Amnesia.Game;

/// Le richieste esplicite non sono decisioni del modello. Stesse condizioni
/// per le azioni visibili nella UI e per il controllo del turno nel dominio.
public static class ConsegneNarrative
{
    private static readonly string[] Articoli = { "il ", "lo ", "la ", "i ", "gli ", "le ", "un ", "uno ", "una ", "l'", "un'", "l\u2019", "un\u2019" };

    public static string Didascalia(IEnumerable<string> ricevuti, ItemCatalog? items)
    {
        var nomi = ricevuti.Distinct().Select(id =>
        {
            var nome = items?.Find(id)?.Name;
            if (string.IsNullOrEmpty(nome)) return id;
            foreach (var articolo in Articoli)
                if (nome.StartsWith(articolo, StringComparison.OrdinalIgnoreCase) && nome.Length > articolo.Length)
                    return nome.Substring(articolo.Length);
            return nome;
        }).ToArray();
        return nomi.Length == 0 ? "" : "ricevi: " + string.Join(", ", nomi);
    }

    public static bool AccessoElena(WorldState world) =>
        world.ItemOwners.TryGetValue("due_righe_matteo", out var owner) && owner == "player"
        && world.ShownToNpc("wanda").Contains("due_righe_matteo");

    public static IReadOnlyList<string> Richiedibili(WorldState world, string npcId)
    {
        var id = npcId == "don_carlo" ? "fotografia"
            : npcId == "matteo" && new Register(world).IsEstablished("matteo_confessa")
                ? "due_righe_matteo" : "";
        return id.Length > 0 && !world.ItemOwners.ContainsKey(id)
            ? new[] { id } : Array.Empty<string>();
    }

    public static void PrimoIncontroRosa(WorldState world)
    {
        if (world.Flags.TryGetValue("rosa_consegna_iniziale", out var done) && done) return;
        foreach (var id in new[] { "foglio_indirizzo", "chiave_b17", "taccuino" })
            if (!world.ItemOwners.ContainsKey(id)) world.ItemOwners[id] = "player";
        world.Flags["rosa_consegna_iniziale"] = true;
    }
}
