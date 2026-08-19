using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Llm;

public class LlmConfigTests
{
    private const string ValidConfig =
        "{\"primary_model\": \"deepseek/deepseek-v4-flash-0731\", \"fallback_model\": \"deepseek/deepseek-v3.2\", \"request_timeout_sec\": 45, \"max_tool_passes\": 4}";

    [Test]
    public void UnaConfigurazioneValidaSiLegge()
    {
        var parsed = LlmConfig.FromJson(ValidConfig);

        Assert.That(parsed.IsOk, Is.True);
        Assert.That(parsed.Value!.PrimaryModel, Is.EqualTo("deepseek/deepseek-v4-flash-0731"));
        Assert.That(parsed.Value!.FallbackModel, Is.EqualTo("deepseek/deepseek-v3.2"));
        Assert.That(parsed.Value!.RequestTimeoutSec, Is.EqualTo(45));
        Assert.That(parsed.Value!.MaxToolPasses, Is.EqualTo(4));
    }

    [Test]
    public void CioCheMancaHaUnValoreDiPartenzaSensato()
    {
        var parsed = LlmConfig.FromJson("{\"primary_model\": \"m\"}");

        Assert.That(parsed.Value!.RequestTimeoutSec, Is.EqualTo(45));
        Assert.That(parsed.Value!.MaxToolPasses, Is.EqualTo(4));
        Assert.That(parsed.Value!.FallbackModel, Is.Empty);
    }

    [Test]
    public void SoloIModelliAnthropicVoglionoILoroMarcatoriDiCache()
    {
        var config = LlmConfig.FromJson(ValidConfig).Value!;

        Assert.That(config.SupportsCacheControl("anthropic/claude-sonnet-4.5"), Is.True);
        Assert.That(config.SupportsCacheControl("deepseek/deepseek-v4-flash-0731"), Is.False);
    }

    [Test]
    public void UnaConfigurazioneMalformataVieneRifiutata()
    {
        Assert.That(LlmConfig.FromJson("not json").Code, Is.EqualTo("invalid_config"));
        Assert.That(LlmConfig.FromJson("7").Code, Is.EqualTo("invalid_config"), "JSON valido ma non un oggetto");
    }

    [Test]
    public void SenzaModelloPrimarioNonSiParte()
    {
        var parsed = LlmConfig.FromJson("{\"fallback_model\": \"m\"}");

        Assert.That(parsed.IsOk, Is.False);
        Assert.That(parsed.Code, Is.EqualTo("invalid_config"));
    }

    [Test]
    public void UnFileCheNonCEUnFallimentoNonUnEccezione()
    {
        var loaded = LlmConfig.Load(Path.Combine(Path.GetTempPath(), "amnesia-llm-config-" + Guid.NewGuid().ToString("N")));

        Assert.That(loaded.IsOk, Is.False);
        Assert.That(loaded.Code, Is.EqualTo("invalid_config"));
    }
}
