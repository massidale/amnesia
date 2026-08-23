using Amnesia;
using Amnesia.Core;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// I cancelli che tengono in piedi l'ordine della trama: la prova della cava fa
/// cedere Matteo sul rito e sul ratto, e solo la giacca — riconosciuta come sua
/// — apre la confessione del 1985 e il foglio per Wanda.
public class CancelliTests
{
    private static PositionTable Reali() =>
        PositionTable.Load(Path.Combine(TestContext.CurrentContext.TestDirectory, "Amnesia", "fixtures", "positions.json")).Value!;

    [Test]
    public void UnOggettoDelMagazzinoFaCedereMatteoSulRitoESulRatto()
    {
        var world = new WorldState();
        world.MarkShown("matteo", "frase");
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M1"));

        // Basta un solo oggetto della cava sul banco — qui il quaderno di
        // Vittorio — e la scampagnata non regge: rito e ratto insieme.
        world.MarkShown("matteo", "quaderno_vittorio");
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M2"),
            "una prova che lassù c'era una bambina, e Matteo racconta di averla portata via");
        Assert.That(Reali().Granted("matteo", world), Does.Contain("matteo_la_porto_via"));
        Assert.That(Reali().Granted("matteo", world), Does.Contain("elena_viva"));
    }

    [Test]
    public void SoloLaGiaccaRiconosciutaComeSuaFaConfessareMatteo()
    {
        var world = new WorldState();
        foreach (var itemId in new[] { "frase", "braccialetto" })
        {
            world.MarkShown("matteo", itemId);
        }
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M2"));

        // La giacca in mano, ma non ancora accertata di chi sia: Matteo regge.
        world.MarkShown("matteo", "giacca");
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M2"),
            "una giacca qualunque non lo tocca finché non si sa di chi è");

        // Qualcuno la riconosce — la conta da sola, come una cosa vista mille
        // volte — e adesso, in faccia, non c'è versione che regga.
        new Register(world).Record("rosa", "giacca_e_di_matteo", countsAlone: true);
        Assert.That(Reali().PositionOf("matteo", world), Is.EqualTo("M3"),
            "la giacca sua, mostrata in faccia, chiude la scala");
        Assert.That(Reali().ConsegnateFinora("matteo", world), Does.Contain("due_righe_matteo"),
            "e concede il foglio per Wanda");
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
    public void NinoTieneLaGiaccaFinoAlloStalloConMatteo()
    {
        var world = new WorldState();
        Assert.That(Reali().PositionOf("nino", world), Is.EqualTo("N0"));
        Assert.That(Reali().ConsegnateFinora("nino", world), Is.Empty,
            "prima dello stallo la giacca non esiste per il giocatore");

        // Lo stallo non e' un contatore: e' questo stato — Elena viva, e Matteo
        // che dice di no.
        var registro = new Register(world);
        registro.Record("matteo", "elena_viva", countsAlone: true);
        registro.Record("matteo", "matteo_non_dice_dove", countsAlone: true);

        Assert.That(Reali().PositionOf("nino", world), Is.EqualTo("N1"));
        Assert.That(Reali().ConsegnateFinora("nino", world), Does.Contain("giacca"));
    }
}
