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
    private bool CanSay(WorldState world, string speakerId, string declarationId)
    {
        if (_positions.HasLadder(speakerId))
        {
            return _positions.Granted(speakerId, world).Contains(declarationId);
        }

        var declaration = _declarations.Find(declarationId);
        if (declaration is null || !declaration.Sources.Contains(speakerId))
        {
            return false;
        }

        var shown = world.ShownToNpc(speakerId);
        return declaration.RequiresShown.All(shown.Contains);
    }
}
