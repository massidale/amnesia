using Amnesia;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

public class ReactionTableTests
{
    private static ReactionTable Tavola() =>
        ReactionTable.FromJson("""
        {"Reazioni": {
          "chiunque": {"sempre": {"chiave": "non la conosci"}},
          "anna": {
            "A0": {"chiave": "non dici niente", "foto": "scampagnate"},
            "A1": {"chiave": "di' del magazzino"},
            "sempre": {"lapide": "la conosce tutto il paese"}
          }
        }}
        """).Value!;

    [Test]
    public void IlGradinoPiuAltoVince()
    {
        Assert.That(Tavola().Reazione("anna", new[] { "A0", "A1" }, "chiave"),
            Is.EqualTo("di' del magazzino"));
    }

    [Test]
    public void UnGradinoCheNonRidefinisceLasciaLaVoceDiSotto()
    {
        // A1 non parla della foto: vale ancora la scampagnata di A0.
        Assert.That(Tavola().Reazione("anna", new[] { "A0", "A1" }, "foto"),
            Is.EqualTo("scampagnate"));
    }

    [Test]
    public void SenzaVoceDelPersonaggioParlaIlDefault()
    {
        Assert.That(Tavola().Reazione("nino", System.Array.Empty<string>(), "chiave"),
            Is.EqualTo("non la conosci"));
    }

    [Test]
    public void LaVoceSempreDelPersonaggioBatteIlDefault()
    {
        Assert.That(Tavola().Reazione("anna", new[] { "A0" }, "lapide"),
            Is.EqualTo("la conosce tutto il paese"));
    }

    [Test]
    public void UnOggettoSenzaCopioneRestituisceVuoto()
    {
        Assert.That(Tavola().Reazione("anna", new[] { "A0" }, "ombrello"), Is.Empty);
    }

    [Test]
    public void IlCopioneVeroSiCaricaEHaLeVociDeiQuattro()
    {
        var vera = ReactionTable.Load(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..",
                "content", "amnesia", "reazioni.json"));
        Assert.That(vera.IsOk, Is.True, vera.Message);
        // Le voci che la demo ha dimostrato mancare: la chiave ad Anna sul
        // gradino della frase deve dire del magazzino, subito.
        Assert.That(vera.Value!.Reazione("anna", new[] { "A0", "A1" }, "chiave_b17"),
            Does.Contain("magazzino"));
        Assert.That(vera.Value!.Reazione("laura", new[] { "L0", "L1", "L2" }, "braccialetto"),
            Does.Contain("rito"));
        Assert.That(vera.Value!.Reazione("matteo", new[] { "M0" }, "frase"),
            Does.Contain("cosa ti ricordi"));
        Assert.That(vera.Value!.Reazione("piero", System.Array.Empty<string>(), "braccialetto"),
            Does.Contain("Non sai di chi sia"));
    }
}
