using Amnesia;
using Amnesia.Core;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// I cancelli che non guardano gli oggetti. Sono quelli che tengono in piedi
/// l'ordine emotivo del finale, e prima esistevano solo sulla carta.
public class CancelliTests
{
    private static PositionTable Reali() =>
        PositionTable.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "Amnesia", "fixtures", "positions.json")).Value!;

    [Test]
    public void LaBustaDelPadreNonEsceFinchePrimaElenaNonRisultaViva()
    {
        var world = new WorldState();
        world.MarkShown("don_carlo", "braccialetto");
        Assert.That(Reali().PositionOf("don_carlo", world), Is.EqualTo("C1"));

        // Mostrargli il foglio con l'indirizzo e' una mossa naturale del primo
        // atto, e prima apriva la busta: la lettera del padre cadeva mezza partita
        // prima di Matteo, e con lei tutto l'ordine del finale.
        world.MarkShown("don_carlo", "foglio_indirizzo");
        Assert.That(Reali().PositionOf("don_carlo", world), Is.EqualTo("C1"),
            "un oggetto in mano non e' una cosa accertata");

        new Register(world).Record("matteo", "elena_viva", countsAlone: true);
        Assert.That(Reali().PositionOf("don_carlo", world), Is.EqualTo("C2"),
            "la condizione che Andrea ha scritto e' venire a chiedere di una persona VIVA");
    }

    [Test]
    public void MatteoCedeAlConfrontoENonAUnOggetto()
    {
        var world = new WorldState();
        foreach (var itemId in new[] { "frase", "braccialetto", "registro", "diario", "foglio_indirizzo" })
        {
            world.MarkShown("matteo", itemId);
        }
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M2"),
            "si puo' arrivare con le mani piene e non ottenere niente");

        world.MarkConfrontoShown("matteo", "anna_solo_matteo", "matteo_ero_gia_sceso");
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M4"),
            "cede a due frasi che erano in piazza da ventun anni");
    }

    [Test]
    public void IlVersoDelConfrontoNonConta()
    {
        var world = new WorldState();
        world.MarkShown("matteo", "frase");
        world.MarkShown("matteo", "braccialetto");
        world.MarkConfrontoShown("matteo", "matteo_ero_gia_sceso", "anna_solo_matteo");

        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M3"),
            "accostare A e B e' la stessa mossa che accostare B e A");
    }

    [Test]
    public void UnConfrontoValePerChiSeLoEVistoDavanti()
    {
        var world = new WorldState();
        world.MarkShown("matteo", "frase");
        world.MarkShown("matteo", "braccialetto");
        world.MarkConfrontoShown("anna", "anna_solo_matteo", "matteo_ero_gia_sceso");

        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M2"),
            "convincere Anna non convince Matteo, ed e' quello il lavoro da fare in bottega");
    }

    [Test]
    public void UnCampoScrittoMaleNonEUnCancelloCheNonEsiste()
    {
        var broken = PositionTable.FromJson(
            "{\"positions\":{\"matteo\":[{\"id\":\"M0\",\"grants\":[],\"requires_declarated\":[\"elena_viva\"]}]}}");

        Assert.That(broken.IsOk, Is.False, "System.Text.Json ignorerebbe la chiave in silenzio");
        Assert.That(broken.Code, Is.EqualTo("unknown_position_field"));
    }

    [Test]
    public void UnaConfessioneControSeStessiValeDaSola()
    {
        var world = new WorldState();
        var register = new Register(world);

        register.Record("matteo", "elena_viva", countsAlone: true);

        Assert.That(register.IsEstablished("elena_viva"), Is.True,
            "nessuno la conferma, perche' l'unico altro che potrebbe non ne ha nessun motivo");
        register.Record("anna", "corpo_mai_trovato");
        Assert.That(register.IsEstablished("corpo_mai_trovato"), Is.False,
            "e l'eccezione resta un'eccezione: tutto il resto vuole due bocche");
    }
}
