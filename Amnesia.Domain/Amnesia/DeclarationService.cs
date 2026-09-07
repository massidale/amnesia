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

    /// Tutto cio' che questa persona potrebbe dire nell'arco della partita —
    /// non adesso: mai. Serve a ritagliarle addosso il vocabolario chiuso dello
    /// strumento, e per questo non deve dipendere dallo stato: un elenco che
    /// cambia a ogni gradino ricomprerebbe il prefisso del prompt ogni volta.
    /// Il gradino corrente di un personaggio, per chi deve accorgersi che un
    /// turno l'ha fatto salire. Vuoto per chi non ha scala.
    public string PositionOf(string npcId, WorldState world) => _positions.PositionOf(npcId, world);

    /// Gli oggetti che i gradini raggiunti hanno da consegnare al giocatore.
    public IReadOnlyList<string> ConsegnateFinora(string npcId, WorldState world) =>
        _positions.ConsegnateFinora(npcId, world);

    public IReadOnlyList<string> EverSayable(string npcId)
    {
        if (_positions.HasLadder(npcId))
        {
            var tutte = new List<string>();
            foreach (var id in _positions.AllGrants(npcId))
            {
                if (!tutte.Contains(id))
                {
                    tutte.Add(id);
                }
            }
            return tutte.Concat(_declarations.Ids.Where(id =>
            {
                var d = _declarations.Find(id)!;
                return d.Sources.Contains(npcId) && (d.RequiresShown.Count > 0 || d.RequiresAnyShown.Count > 0);
            })).Distinct().ToList();
        }
        return _declarations.Ids.Where(id => _declarations.Find(id)!.Sources.Contains(npcId)).ToList();
    }

    /// Il testo canonico di una dichiarazione, come lo direbbe quella persona.
    /// Serve al motore quando il modello segnala una cosa e non la scrive.
    public string TextOf(string declarationId, string speakerId) =>
        _declarations.TextOf(declarationId, speakerId);

    /// Cosa questo personaggio puo' dire ADESSO, al suo gradino corrente. E'
    /// il sottoinsieme dell'enum che il glossario dello strumento puo' spiegare
    /// per esteso senza svelare i gradini che non ha ancora raggiunto.
    public IReadOnlyList<string> SayableNow(string npcId, WorldState world) =>
        Sayable(_declarations, _positions, npcId, world);

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
        var shown = world.ShownToNpc(npcId);

        if (positions.HasLadder(npcId))
        {
            // La scala decide cosa il personaggio ha, gradino per gradino. Ma un
            // riconoscimento legato a un OGGETTO — «questa giacca e' di Matteo» —
            // non e' un gradino: lo fa chiunque conosca l'oggetto, appena lo vede.
            // Percio' a chi ha una scala si aggiunge, oltre a cio' che la scala
            // concede, ogni dichiarazione di cui e' fonte che dipende da un oggetto
            // sul banco E che la sua scala non governa gia' — altrimenti si
            // scavalcherebbero i gradini (anna_solo_matteo resta cosa di A2, non
            // della giacca).
            var governateDallaScala = new HashSet<string>(positions.AllGrants(npcId));
            var perOggetto = declarations.Ids.Where(id =>
            {
                var d = declarations.Find(id);
                return d is not null
                    && d.Sources.Contains(npcId)
                    && (d.RequiresShown.Count > 0 || d.RequiresAnyShown.Count > 0)
                    && d.RequiresShown.All(shown.Contains)
                    && (d.RequiresAnyShown.Count == 0 || d.RequiresAnyShown.Any(shown.Contains))
                    && !governateDallaScala.Contains(id);
            });
            return positions.Granted(npcId, world).Concat(perOggetto).Distinct().ToList();
        }

        return declarations.Ids
            .Where(id =>
            {
                var declaration = declarations.Find(id);
                return declaration is not null
                    && declaration.Sources.Contains(npcId)
                    && (declaration.RequiresAnyShown.Count == 0 || declaration.RequiresAnyShown.Any(shown.Contains))
                    && declaration.RequiresShown.All(shown.Contains);
            })
            .ToList();
    }
}
