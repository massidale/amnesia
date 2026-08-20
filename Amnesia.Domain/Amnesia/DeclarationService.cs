using System.Linq;
using Amnesia.Core;

namespace Amnesia;

/// Cosa succede quando un personaggio dichiara qualcosa.
///
/// Il vocabolario e' chiuso: il modello non scrive un fatto, sceglie un id da una
/// lista. Ma un modello puo' sempre scegliere l'id sbagliato, e la decisione su
/// cosa risulti detto non e' mai sua — un personaggio puo' dichiarare solo cio'
/// che la sua posizione gli concede, e solo cio' che sta nella tabella.
public sealed class DeclarationService
{
    private readonly DeclarationTable _declarations;
    private readonly PositionTable _positions;

    public DeclarationService(DeclarationTable declarations, PositionTable positions)
    {
        _declarations = declarations;
        _positions = positions;
    }

    /// Il testo canonico di una dichiarazione, come lo direbbe quella persona.
    /// Serve al motore quando il modello segnala una cosa e non la scrive.
    public string TextOf(string declarationId, string speakerId) =>
        _declarations.TextOf(declarationId, speakerId);

    public Result Declare(WorldState world, string speakerId, string declarationId)
    {
        if (!_declarations.Has(declarationId))
        {
            return Result.Fail("unknown_declaration", $"dichiarazione sconosciuta: {declarationId}");
        }
        if (!CanSay(world, speakerId, declarationId))
        {
            return Result.Fail("declaration_not_granted", "non e' cosa che questo personaggio possa dire adesso");
        }
        new Register(world).Record(speakerId, declarationId, _declarations.Find(declarationId)?.CountsAlone ?? false);
        return Result.Ok(WorldEvent.Create(
            "declared", speakerId, world.Minute, ("declaration_id", declarationId)));
    }

    /// Chi ha una scala e' un personaggio guardingo, e puo' dire solo cio' che il
    /// gradino raggiunto gli concede.
    ///
    /// Chi NON ha una scala non e' un personaggio a cui manca qualcosa: e' uno che
    /// non nasconde niente — Rosa, Nino, Wanda. Per lui il solo vincolo e' quello
    /// che vale per chiunque: essere una fonte di quella dichiarazione, e che le
    /// sue precondizioni siano soddisfatte. Senza questa distinzione un personaggio
    /// senza scala resterebbe muto attraverso lo strumento, che e' l'esatto
    /// contrario di cio' che e'.
    private bool CanSay(WorldState world, string speakerId, string declarationId) =>
        Sayable(_declarations, _positions, speakerId, world).Contains(declarationId);

    /// Cosa questo personaggio, adesso, e' in grado di dire.
    ///
    /// E' la stessa lista che il motore accetta quando lui dichiara e che il
    /// prompt gli mette davanti: un personaggio deve *avere* le righe che gli
    /// e' permesso dire, o le improvvisa — e uno che improvvisa su una cosa
    /// finisce per improvvisare anche su tutto il resto.
    public static IReadOnlyList<string> Sayable(
        DeclarationTable declarations, PositionTable positions, string npcId, WorldState world)
    {
        if (positions.HasLadder(npcId))
        {
            return positions.Granted(npcId, world);
        }

        var shown = world.ShownToNpc(npcId);
        return declarations.Ids
            .Where(id =>
            {
                var declaration = declarations.Find(id);
                return declaration is not null
                    && declaration.Sources.Contains(npcId)
                    && declaration.RequiresShown.All(shown.Contains);
            })
            .ToList();
    }
}
