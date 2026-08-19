using Amnesia.Core;
using Amnesia.Persistence;
using NUnit.Framework;

namespace Amnesia.Tests.Persistence;

public class SaveCodecTests
{
    [Test]
    public void UnSalvataggioTornaIndietroIntero()
    {
        var world = new WorldState { Minute = 700 };
        world.MarkShown("matteo", "frase");

        var decoded = new SaveCodec().Decode(new SaveCodec().Encode(world));

        Assert.That(decoded.IsOk, Is.True);
        Assert.That(decoded.Value!.Minute, Is.EqualTo(700));
        Assert.That(decoded.Value!.ShownToNpc("matteo"), Is.EqualTo(new[] { "frase" }));
    }

    [Test]
    public void UnSalvataggioDiUnaVersioneSbagliataVieneRifiutatoAVoceAlta()
    {
        var decoded = new SaveCodec().Decode("{\"Version\":99,\"State\":\"{}\"}");

        Assert.That(decoded.IsOk, Is.False);
        Assert.That(decoded.Code, Is.EqualTo("unsupported_save_version"), "e non si carica a meta'");
    }

    [Test]
    public void SpazzaturaNonFaEsplodereNiente()
    {
        var decoded = new SaveCodec().Decode("non sono json");

        Assert.That(decoded.IsOk, Is.False);
        Assert.That(decoded.Code, Is.EqualTo("invalid_json"));
    }
}
