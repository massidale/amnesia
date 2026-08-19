using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.World;

public class RoutineTableTests
{
    [Test]
    public void OgnunoHaIlSuoPostoAllaSuaOra()
    {
        var table = TestVillage.Routines();
        Assert.That(table.PlaceFor("anna", 540), Is.EqualTo("cucina"), "Anna comincia in cucina");
        Assert.That(table.PlaceFor("bruno", 540), Is.EqualTo("bottega"), "Bruno apre la bottega");
        Assert.That(table.PlaceFor("bruno", 779), Is.EqualTo("bottega"), "e c'e' ancora un minuto prima di uscire");
        Assert.That(table.PlaceFor("bruno", 780), Is.EqualTo("strada"), "alle 13 esce");
        Assert.That(table.PlaceFor("bruno", 900), Is.EqualTo("bottega"), "e piu' tardi e' rientrato");
    }

    [Test]
    public void ChiNonHaRoutineNonEAttesoDaNessunaParte()
    {
        var table = TestVillage.Routines();
        Assert.That(table.PlaceFor("nessuno", 540), Is.EqualTo(""), "un personaggio senza routine non ha un posto atteso");
        Assert.That(table.PlaceFor("bruno", 0), Is.EqualTo(""), "e prima della prima voce non ce l'ha nemmeno lui");
        Assert.That(RoutineTable.Empty().PlaceFor("bruno", 540), Is.EqualTo(""));
    }

    [Test]
    public void LeRoutineSiLeggonoAncheDaUnaStringa()
    {
        var loaded = RoutineTable.FromJson("""
        {"carla": [{"from_minute": 600, "place": "chiesa"}]}
        """);
        Assert.That(loaded.IsOk, Is.True);
        Assert.That(loaded.Value!.PlaceFor("carla", 600), Is.EqualTo("chiesa"));
        Assert.That(loaded.Value!.PlaceFor("carla", 599), Is.EqualTo(""));
    }

    [Test]
    public void RoutineCheNonCiSonoONonSiLeggonoNonPassanoInSilenzio()
    {
        var missing = RoutineTable.Load(TestVillage.FixturePath("nessuna_routine.json"));
        Assert.That(missing.IsOk, Is.False);
        Assert.That(missing.Code, Is.EqualTo("routines_not_found"));

        var broken = RoutineTable.FromJson("{\"anna\": [");
        Assert.That(broken.IsOk, Is.False);
        Assert.That(broken.Code, Is.EqualTo("invalid_routines"));
    }
}
