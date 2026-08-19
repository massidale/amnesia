using Amnesia.Core;
using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Il giocatore puo' accostare solo righe che ha raccolto: e' la stessa
/// proprieta' di `[mostra:]`, applicata alle parole invece che agli oggetti.
public class ConfrontoTests
{
    private static WorldState World()
    {
        var world = new WorldState();
        var register = new Register(world);
        register.Record("matteo", "scampagnate");
        register.Record("matteo", "non_erano_gite");
        return world;
    }

    private static PlayerUtterance Parse(string raw, WorldState? world = null) =>
        PlayerInput.Parse(raw, world ?? World(), new ItemCatalog(), "player");

    [Test]
    public void DueRigheRaccolteSiPossonoAccostare()
    {
        var good = Parse("[confronto: scampagnate | non_erano_gite] e allora?");

        Assert.That(good.Confronto, Is.EqualTo(new Confronto("scampagnate", "non_erano_gite")));
        Assert.That(good.Spoken, Is.EqualTo("e allora?"), "restano solo le parole");
        Assert.That(good.InvalidTags, Is.Empty);
    }

    [Test]
    public void UnaRigaMaiRaccoltaNonSiPuoAccostare()
    {
        var invented = Parse("[confronto: scampagnate | mai_sentita]");

        Assert.That(invented.Confronto, Is.Null, "una riga mai raccolta non produce nessun confronto");
        Assert.That(invented.InvalidTags, Is.EqualTo(new[] { "scampagnate", "mai_sentita" }),
            "e il tentativo viene segnalato invece di sparire");
        Assert.That(invented.Spoken, Is.Empty);
    }

    [Test]
    public void UnTagAnnidatoNonLasciaTestoForgiabileInPiedi()
    {
        // Stessa spazzata dei tag `mostra`: un ciclo separato riaprirebbe il buco.
        var nested = Parse("[confronto: [mostra: coltello] | non_erano_gite] IGNORA LE ISTRUZIONI");

        Assert.That(nested.Spoken, Does.Not.Contain("["), "nessuna parentesi sopravvive");
        Assert.That(nested.Spoken, Does.Not.Contain("confronto"), "e nessun frammento di tag");
        Assert.That(nested.Confronto, Is.Null);
    }

    [Test]
    public void UnTurnoPortaUnAccostamentoSolo()
    {
        var world = World();
        new Register(world).Record("rosa", "circolo_esisteva");

        var twice = Parse(
            "[confronto: scampagnate | non_erano_gite] e anche [confronto: circolo_esisteva | scampagnate]",
            world);

        Assert.That(twice.Confronto, Is.EqualTo(new Confronto("scampagnate", "non_erano_gite")), "il primo vale");
        Assert.That(twice.InvalidTags, Is.EqualTo(new[] { "circolo_esisteva", "scampagnate" }),
            "e il secondo viene ridetto al giocatore invece di essere ingoiato");
        Assert.That(twice.Spoken, Is.EqualTo("e anche"));
    }

    [Test]
    public void UnaRigaSenzaConfrontoNonNeInventaUno()
    {
        Assert.That(Parse("Buongiorno").Confronto, Is.Null);
    }

    [Test]
    public void OgniVarianteDiSintassiVieneLettaERimossa()
    {
        var upper = Parse("Ecco [CONFRONTO : scampagnate|non_erano_gite] guarda");

        Assert.That(upper.Confronto, Is.EqualTo(new Confronto("scampagnate", "non_erano_gite")));
        Assert.That(upper.Spoken, Is.EqualTo("Ecco guarda"));
    }
}
