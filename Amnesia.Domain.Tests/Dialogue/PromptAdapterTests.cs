using System.Text.Json;
using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Dialogue;

public class PromptAdapterTests
{
    private static string Wire(PromptMessage message) => JsonSerializer.Serialize(message.ToWire());

    [Test]
    public void UnMessaggioDiUnaParteSolaRestaPiatto()
    {
        var json = Wire(PromptMessage.Plain(ChatRole.User, "buongiorno"));

        Assert.That(json, Does.Contain("\"content\":\"buongiorno\""), "una stringa, non una lista");
        Assert.That(json, Does.Not.Contain("cache_control"));
    }

    [Test]
    public void UnPrefissoConBreakpointArrivaAPezziEIlBreakpointSopravvive()
    {
        var prefix = new PromptMessage(ChatRole.System, new[]
        {
            new PromptPart("le regole"),
            new PromptPart("la scheda", CacheBreakpoint: true),
        });

        var json = Wire(prefix);

        Assert.That(json, Does.Contain("\"content\":["), "la forma a blocchi, non una stringa concatenata");
        Assert.That(json, Does.Contain("le regole"));
        Assert.That(json, Does.Contain("la scheda"));
        Assert.That(json, Does.Contain("\"cache_control\":{\"type\":\"ephemeral\"}"),
            "senza questo ogni richiesta ricalcola il prefisso invece di riusarlo");
    }

    [Test]
    public void IlBreakpointStaSullaParteGiustaENonSullAltra()
    {
        var prefix = new PromptMessage(ChatRole.System, new[]
        {
            new PromptPart("le regole"),
            new PromptPart("la scheda", CacheBreakpoint: true),
        });

        using var document = JsonDocument.Parse(Wire(prefix));
        var blocks = document.RootElement.GetProperty("content");

        Assert.That(blocks[0].TryGetProperty("cache_control", out _), Is.False, "le regole non portano il taglio");
        Assert.That(blocks[1].TryGetProperty("cache_control", out _), Is.True, "lo porta la scheda, che chiude il prefisso");
    }

    [Test]
    public void UnaParteSolaMaConBreakpointNonVienePiattita()
    {
        var json = Wire(new PromptMessage(ChatRole.System, new[] { new PromptPart("tutto insieme", CacheBreakpoint: true) }));

        Assert.That(json, Does.Contain("cache_control"), "un breakpoint non si perde mai, nemmeno da solo");
    }
}
