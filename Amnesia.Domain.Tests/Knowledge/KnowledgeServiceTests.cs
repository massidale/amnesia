using Amnesia.Core;
using Amnesia.Knowledge;
using NUnit.Framework;

namespace Amnesia.Tests.Knowledge;

public class KnowledgeServiceTests
{
    [Test]
    public void CioCheUnPersonaggioNonSaPerLuiNonEsiste()
    {
        var knowledge = new KnowledgeService(new WorldState());
        Assert.That(knowledge.Knows("anna", "elena_viva"), Is.False);
        Assert.That(knowledge.ContextFor("anna"), Is.Empty);
    }

    [Test]
    public void LeColonneNonSiToccanoFraLoro()
    {
        var world = new WorldState();
        var knowledge = new KnowledgeService(world);

        knowledge.RevealFact("matteo", "elena_viva", "e' viva", "vissuto", 1.0);

        Assert.That(knowledge.Knows("matteo", "elena_viva"), Is.True);
        Assert.That(knowledge.Knows("anna", "elena_viva"), Is.False, "sapere una cosa non la fa sapere a nessun altro");
    }

    [Test]
    public void UnAffermazioneDelGiocatoreRestaUnAffermazioneAltrui()
    {
        var world = new WorldState();
        var knowledge = new KnowledgeService(world);

        knowledge.RecordClaim("anna", "player", "elena_viva", "dice che Elena e' viva", 0.3);

        var belief = knowledge.ContextFor("anna").Single();
        Assert.That(belief.SourceId, Is.EqualTo("player"), "la fonte e' chi l'ha detta, non il mondo");
        Assert.That(belief.Confidence, Is.EqualTo(0.3));
    }

    [Test]
    public void LaConfidenzaNonEsceMaiDaZeroUno()
    {
        var knowledge = new KnowledgeService(new WorldState());
        knowledge.RevealFact("anna", "a", "x", "s", 4.0);
        knowledge.RevealFact("anna", "b", "y", "s", -2.0);

        var beliefs = knowledge.ContextFor("anna").ToList();
        Assert.That(beliefs[0].Confidence, Is.EqualTo(1.0));
        Assert.That(beliefs[1].Confidence, Is.EqualTo(0.0));
    }
}
