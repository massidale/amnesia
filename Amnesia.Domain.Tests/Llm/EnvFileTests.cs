using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Llm;

public class EnvFileTests
{
    // Un valore finto, con la forma di una chiave ma senza esserlo: la chiave
    // vera non entra in nessuna fixture di questo progetto.
    private const string FakeKey = "sk-or-fixture-non-reale";

    [Test]
    public void LaChiaveVienePresaDalTestoDelFile()
    {
        var key = EnvFile.ParseKey($"# commento\nOPENROUTER_KEY={FakeKey}\nALTRA=x\n", "OPENROUTER_KEY");

        Assert.That(key.IsOk, Is.True);
        Assert.That(key.Value, Is.EqualTo(FakeKey));
    }

    [Test]
    public void GliSpaziIntornoAllUgualeNonContano()
    {
        var spaced = EnvFile.ParseKey("OPENROUTER_KEY = sk-or-spaziata", "OPENROUTER_KEY");

        Assert.That(spaced.IsOk, Is.True);
        Assert.That(spaced.Value, Is.EqualTo("sk-or-spaziata"));
    }

    [Test]
    public void LeVirgoletteIntornoAlValoreVengonoTolte()
    {
        Assert.That(EnvFile.ParseKey("OPENROUTER_KEY=\"sk-or-quotata\"", "OPENROUTER_KEY").Value, Is.EqualTo("sk-or-quotata"));
        Assert.That(EnvFile.ParseKey("OPENROUTER_KEY = \"sk-or-quotata-spaziata\"", "OPENROUTER_KEY").Value, Is.EqualTo("sk-or-quotata-spaziata"));
    }

    [Test]
    public void UnaRigaCommentataNonVale()
    {
        var key = EnvFile.ParseKey($"#OPENROUTER_KEY=sk-or-commentata\nOPENROUTER_KEY={FakeKey}", "OPENROUTER_KEY");

        Assert.That(key.Value, Is.EqualTo(FakeKey));
    }

    [Test]
    public void UnaChiaveAssenteVieneSegnalataESiRestaSenza()
    {
        var missing = EnvFile.ParseKey("ALTRA=x", "OPENROUTER_KEY");

        Assert.That(missing.IsOk, Is.False);
        Assert.That(missing.Code, Is.EqualTo("missing_api_key"));
    }

    [Test]
    public void UnaChiaveConNomeSimileNonVieneScambiataPerQuellaGiusta()
    {
        Assert.That(EnvFile.ParseKey("OPENROUTER_KEY_EXTRA=x", "OPENROUTER_KEY").Code, Is.EqualTo("missing_api_key"));
    }

    [Test]
    public void UnValoreVuotoNonEUnaChiave()
    {
        Assert.That(EnvFile.ParseKey("OPENROUTER_KEY=\nOPENROUTER_KEY=\"\"", "OPENROUTER_KEY").Code, Is.EqualTo("missing_api_key"));
    }

    [Test]
    public void IlMessaggioDErroreNonRipeteNienteDiCioCheHaLetto()
    {
        var missing = EnvFile.ParseKey($"ALTRO_SEGRETO={FakeKey}", "OPENROUTER_KEY");

        Assert.That(missing.Message, Does.Not.Contain(FakeKey), "un errore finisce nei log, una chiave no");
    }

    [Test]
    public void UnFileCheNonCEUnFallimentoNonUnEccezione()
    {
        var loaded = EnvFile.LoadOpenRouterKey(Path.Combine(Path.GetTempPath(), "amnesia-env-che-non-esiste-" + Guid.NewGuid().ToString("N")));

        Assert.That(loaded.IsOk, Is.False);
        Assert.That(loaded.Code, Is.EqualTo("missing_api_key"));
    }

    [Test]
    public void LaChiaveVieneLettaDaUnFileDiFixture()
    {
        var path = Path.Combine(Path.GetTempPath(), "amnesia-env-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, $"# fixture\nOPENROUTER_KEY={FakeKey}\n");
        try
        {
            var loaded = EnvFile.LoadOpenRouterKey(path);

            Assert.That(loaded.IsOk, Is.True);
            Assert.That(loaded.Value, Is.EqualTo(FakeKey));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
