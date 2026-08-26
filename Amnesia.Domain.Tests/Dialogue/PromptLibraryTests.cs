using Amnesia.Dialogue;
using NUnit.Framework;

namespace Amnesia.Tests.Dialogue;

public class PromptLibraryTests
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
    public void UnaSchedaPerGradinoStaInSottocartella()
    {
        var schede = PromptLibrary.Load(PromptsDir());
        // La scheda di gradino: chiave con la barra, la sottocartella e' la persona.
        Assert.That(schede.Keys, Does.Contain("matteo/M2"));
        Assert.That(schede["matteo/M2"], Does.Contain("Matteo Sardi"));
        // La scheda unica resta al primo livello.
        Assert.That(schede.Keys, Does.Contain("rosa"));
        // Le regole non sono una scheda.
        Assert.That(schede.Keys, Does.Not.Contain("rules"));
    }

    [Test]
    public void LaPersonaELaSottocartella()
    {
        Assert.That(PromptLibrary.PersonaDi("matteo/M2"), Is.EqualTo("matteo"));
        Assert.That(PromptLibrary.PersonaDi("matteo/M0"), Is.EqualTo("matteo"));
        Assert.That(PromptLibrary.PersonaDi("anna"), Is.EqualTo("anna"));
    }
}
