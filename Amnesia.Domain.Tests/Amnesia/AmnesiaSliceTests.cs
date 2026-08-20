using Amnesia.Core;
using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// La fetta intera, senza rete e con le risposte del modello simulate: il motore
/// registra cosa e' stato detto, un personaggio cambia posizione per un predicato
/// e non per un contatore, e il giocatore accosta solo righe che ha raccolto.
public class AmnesiaSliceTests
{
    [Test]
    public void UnaPartitaInCuiIlGiocatoreDiceLaFrase()
    {
        var world = new WorldState();
        var register = new Register(world);
        var positions = TestDeclarations.Positions();
        var declarations = new DeclarationService(TestDeclarations.Table(), positions);

        // 1. Il giocatore mette la fotografia in mano a Rosa, e Rosa dice che quel
        // gruppo esisteva. Un sostegno solo: non basta.
        world.MarkShown("rosa", "fotografia");
        Assert.That(declarations.Declare(world, "rosa", "circolo_esisteva").IsOk, Is.True,
            "Rosa non ha una scala: le basta essere fonte e avere la fotografia davanti");
        Assert.That(register.IsEstablished("circolo_esisteva"), Is.False, "una bocca sola non stabilisce niente");

        // 2. Matteo, a freddo, tiene la copertura — ed e' tutto quello che ha.
        Assert.That(positions.PositionOf("matteo", world), Is.EqualTo("M0"));
        Assert.That(positions.Granted("matteo", world), Does.Not.Contain("non_erano_gite"),
            "e non ha la riga che la smentisce");
        Assert.That(declarations.Declare(world, "matteo", "scampagnate").IsOk, Is.True);
        Assert.That(declarations.Declare(world, "matteo", "circolo_esisteva").IsOk, Is.False,
            "a M0 quella compagnia di amici non ha nemmeno un nome");

        // 3. Il giocatore gli dice la frase.
        world.MarkShown("matteo", "frase");
        Assert.That(positions.PositionOf("matteo", world), Is.EqualTo("M1"), "la frase lo porta a M1");
        Assert.That(declarations.Declare(world, "matteo", "non_erano_gite").IsOk, Is.True);
        Assert.That(declarations.Declare(world, "matteo", "circolo_esisteva").IsOk, Is.True);
        Assert.That(register.IsEstablished("circolo_esisteva"), Is.True, "due bocche indipendenti stabiliscono");

    }

    [Test]
    public void LaPartitaInCuiIlGiocatoreNonDiceMaiLaFrase()
    {
        // Matteo resta a M0 per sempre, e quella riga non e' mai stata nel suo
        // contesto. Non c'e' niente che possa sfuggirgli, nemmeno sotto pressione.
        var silent = new WorldState();
        var positions = TestDeclarations.Positions();
        var declarations = new DeclarationService(TestDeclarations.Table(), positions);

        Assert.That(positions.PositionOf("matteo", silent), Is.EqualTo("M0"), "senza la frase resta a M0");
        Assert.That(positions.Granted("matteo", silent), Does.Not.Contain("non_erano_gite"), "e la riga non e' mai sua");
        Assert.That(declarations.Declare(silent, "matteo", "non_erano_gite").IsOk, Is.False);

        Assert.That(declarations.Declare(silent, "matteo", "scampagnate").IsOk, Is.True, "la copertura ce l'ha");
    }

}
