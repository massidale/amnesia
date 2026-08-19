using Amnesia.Core;
using NUnit.Framework;

// Il namespace non e' `Amnesia.Tests.Amnesia`: un segmento che ripete la radice
// nasconderebbe il namespace radice a ogni file di questa cartella, e `Amnesia.Core`
// smetterebbe di risolversi.
namespace Amnesia.Tests.Declarations;

/// Le tabelle dei test sono una copia della fetta scritta in `content/amnesia/`:
/// i test girano sulla cartella di output, non sulla radice del repository, e un
/// test che carica il contenuto del gioco dipenderebbe da dove e' stato lanciato.
public static class TestDeclarations
{
    public static string FixturePath(string fileName) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Amnesia", "fixtures", fileName);

    public static DeclarationTable Table()
    {
        var loaded = DeclarationTable.Load(FixturePath("declarations.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    public static PositionTable Positions()
    {
        var loaded = PositionTable.Load(FixturePath("positions.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    /// Un mondo in cui a Matteo e' stato messo davanti quello che gli e' stato
    /// messo davanti, e niente altro.
    public static WorldState World(params string[] shownToMatteo)
    {
        var world = new WorldState();
        foreach (var itemId in shownToMatteo)
        {
            world.MarkShown("matteo", itemId);
        }
        return world;
    }
}
