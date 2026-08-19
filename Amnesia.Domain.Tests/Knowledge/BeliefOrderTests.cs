using Amnesia.Core;
using Amnesia.Knowledge;
using NUnit.Framework;

namespace Amnesia.Tests.Knowledge;

public class BeliefOrderTests
{
    [Test]
    public void LeConvinzioniEscononoNellOrdineInCuiSonoEntrate()
    {
        var knowledge = new KnowledgeService(new WorldState());
        foreach (var id in new[] { "zeta", "alfa", "mu", "beta" })
        {
            knowledge.RevealFact("anna", id, id, "vissuto", 1.0);
        }

        Assert.That(knowledge.ContextFor("anna").Select(b => b.Proposition),
            Is.EqualTo(new[] { "zeta", "alfa", "mu", "beta" }),
            "l'ordine e' cronologico, non alfabetico e non casuale");
    }

    [Test]
    public void CorreggereUnaConvinzioneNonLaSpostaInFondoAllaFila()
    {
        var knowledge = new KnowledgeService(new WorldState());
        knowledge.RevealFact("anna", "a", "prima", "vissuto", 1.0);
        knowledge.RevealFact("anna", "b", "seconda", "vissuto", 1.0);

        knowledge.RevealFact("anna", "a", "prima, corretta", "vissuto", 0.5);

        Assert.That(knowledge.ContextFor("anna").Select(b => b.Proposition),
            Is.EqualTo(new[] { "prima, corretta", "seconda" }),
            "ha corretto una cosa che sapeva gia', non ne ha imparata una nuova");
    }

    [Test]
    public void LOrdineSopravviveAlGiroSulDisco()
    {
        var world = new WorldState();
        var knowledge = new KnowledgeService(world);
        foreach (var id in new[] { "zeta", "alfa", "mu" })
        {
            knowledge.RevealFact("anna", id, id, "vissuto", 1.0);
        }

        var reloaded = new KnowledgeService(WorldState.FromJson(world.ToJson()));

        Assert.That(reloaded.ContextFor("anna").Select(b => b.Proposition),
            Is.EqualTo(new[] { "zeta", "alfa", "mu" }),
            "i byte del prompt devono essere gli stessi dopo un caricamento");
    }
}
