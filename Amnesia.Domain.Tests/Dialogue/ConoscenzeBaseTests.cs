using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Dialogue;

/// I blocchi di conoscenza condivisa vanno solo a chi non ha una posizione sua.
/// La versione del paese sulla tragedia la ricevono le comparse (e Rosa, che ci
/// crede), non i guardinghi (l'hanno nella scheda per-scalino) ne' Wanda/Elena
/// (sanno che Elena e' viva: a loro contraddirebbe la loro versione).
public class ConoscenzeBaseTests
{
    private static string PromptsDir()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "content", "prompts");
            if (File.Exists(Path.Combine(candidate, "rules.md")))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        Assert.Fail("content/prompts non trovata");
        return "";
    }

    [Test]
    public void LeCompareRicevonoLaVersioneDelPaese()
    {
        var conoscenze = ConoscenzeBase.Load(PromptsDir());

        // Il coro, verbatim, nel blocco base.
        Assert.That(conoscenze.PerPersonaggio("beppe"), Does.Contain("non fu ritrovata"));
        Assert.That(conoscenze.PerPersonaggio("teresa"), Does.Contain("non fu ritrovata"));
        // Rosa ci crede: la riceve anche lei.
        Assert.That(conoscenze.PerPersonaggio("rosa"), Does.Contain("non fu ritrovata"));
    }

    [Test]
    public void IMembriConosconoIFattiPubbliciMaWandaEdElenaNo()
    {
        var conoscenze = ConoscenzeBase.Load(PromptsDir());

        // I guardinghi hanno la loro versione nella scheda per-scalino.
        Assert.That(conoscenze.PerPersonaggio("matteo"), Does.Contain("non fu ritrovata"));
        Assert.That(conoscenze.PerPersonaggio("anna"), Does.Contain("non fu ritrovata"));
        // Wanda ed Elena sanno che Elena e' viva: niente versione del paese.
        Assert.That(conoscenze.PerPersonaggio("wanda"), Is.Empty);
        Assert.That(conoscenze.PerPersonaggio("elena"), Is.Empty);
    }
}
