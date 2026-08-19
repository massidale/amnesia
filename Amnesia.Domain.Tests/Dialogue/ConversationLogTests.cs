using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Dialogue;

public class ConversationLogTests
{
    private static ConversationLog Filled()
    {
        var log = new ConversationLog();
        for (var index = 0; index < 6; index++)
        {
            log.Append("giorgio", ChatRole.User, $"u{index}");
            log.Append("giorgio", ChatRole.Assistant, $"a{index}");
        }
        log.Append("giulia", ChatRole.User, "ciao giulia");
        return log;
    }

    [Test]
    public void LaFinestraTieneSoloIlTurniPiuRecentiENelLoroOrdine()
    {
        var window = Filled().Recent("giorgio", 4);

        Assert.That(window.Count, Is.EqualTo(4));
        Assert.That(window[0].Content, Is.EqualTo("u4"));
        Assert.That(window[3].Content, Is.EqualTo("a5"), "l'ultimo messaggio e' il piu' nuovo");
    }

    [Test]
    public void UnaFinestraPiuLungaDellaStoriaRendeLaStoriaIntera()
    {
        Assert.That(Filled().Recent("giulia", 10).Count, Is.EqualTo(1), "le storie non si mescolano fra personaggi");
    }

    [Test]
    public void UnPersonaggioConCuiNonSiEMaiParlatoHaUnaStoriaVuota()
    {
        Assert.That(Filled().Recent("vittorio", 10), Is.Empty);
    }

    [Test]
    public void IlRegistroSopravviveAlGiroSulDisco()
    {
        var log = Filled();

        var restored = ConversationLog.FromJson(log.ToJson());

        Assert.That(restored.Recent("giorgio", 4), Is.EqualTo(log.Recent("giorgio", 4)));
        Assert.That(restored.Recent("giulia", 10).Single().Role, Is.EqualTo(ChatRole.User), "e i ruoli tornano indietro ruoli");
    }
}
