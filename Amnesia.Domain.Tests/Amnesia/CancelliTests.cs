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
    public void MatteoCedeAllaTestimonianzaDiAnnaENonAUnOggetto()
    {
        var world = new WorldState();
        foreach (var itemId in new[] { "frase", "braccialetto", "registro", "diario" })
        {
            world.MarkShown("matteo", itemId);
        }
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M2"),
            "si puo' arrivare con le mani piene e non ottenere niente");

        // Cancello provvisorio, in attesa della prova alla cava: Anna l'ha
        // detto davvero, e una testimone unica senza motivo di mentire vale.
        new Register(world).Record("anna", "anna_solo_matteo", countsAlone: true);
        world.MarkShown("matteo", "foglio_indirizzo");
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M4"));
    }

    [Test]
    public void LaProvaFisicaInFacciaFaCedereMatteo()
    {
        var world = new WorldState();
        foreach (var itemId in new[] { "frase", "braccialetto", "foglio_indirizzo" })
        {
            world.MarkShown("matteo", itemId);
        }
        new Register(world).Record("anna", "anna_solo_matteo", countsAlone: true);
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M4"));

        // La giacca con la segatura nei risvolti, o il referto: una delle due.
        world.MarkShown("matteo", "cartella_clinica");
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M5"),
            "la prova fisica mostrata in faccia chiude la scala");
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

    [Test]
    public void NinoTieneLaSgorbiaFinoAlQuartoAtto()
    {
        var world = new WorldState();
        Assert.That(Reali().PositionOf("nino", world), Is.EqualTo("N0"));
        Assert.That(Reali().ConsegnateFinora("nino", world), Is.Empty,
            "prima dello stallo la sgorbia non esiste per il giocatore");

        // L'atto quarto non e' un contatore: e' questo stato.
        var registro = new Register(world);
        registro.Record("matteo", "elena_viva", countsAlone: true);
        registro.Record("matteo", "matteo_non_dice_dove", countsAlone: true);

        Assert.That(Reali().PositionOf("nino", world), Is.EqualTo("N1"));
        Assert.That(Reali().ConsegnateFinora("nino", world), Does.Contain("scalpello"));
    }

    [Test]
    public void LaSgorbiaEUnaChiaveDellUltimoGradinoDiMatteo()
    {
        var world = new WorldState();
        foreach (var itemId in new[] { "frase", "braccialetto", "foglio_indirizzo" })
        {
            world.MarkShown("matteo", itemId);
        }
        new Register(world).Record("anna", "anna_solo_matteo", countsAlone: true);
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M4"));

        world.MarkShown("matteo", "scalpello");
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M5"),
            "il suo ferro, raccolto accanto al corpo: la terza chiave della serratura");
    }
}
