using System.Text.Json;
using Amnesia;
using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Il catalogo degli strumenti e' un prefisso congelato in cache, la tabella
/// delle dichiarazioni e' un file di dati che si modifica scrivendo contenuto.
/// Sono due cose che nessuno tiene allineate, ed e' il primo posto in cui questo
/// gioco puo' andare alla deriva senza rompersi: aggiungi una dichiarazione ai
/// dati, il modello non la vede nell'enum e non la sceglie mai — non un errore,
/// solo una riga che nessun personaggio dira' mai.
public class CatalogoEDatiTests
{
    private static IReadOnlyList<string> EnumDelCatalogo()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(ToolCatalog.Schemas()));
        foreach (var tool in document.RootElement.EnumerateArray())
        {
            var function = tool.GetProperty("function");
            if (function.GetProperty("name").GetString() != "dichiaro")
            {
                continue;
            }
            return function.GetProperty("parameters").GetProperty("properties")
                .GetProperty("id").GetProperty("enum")
                .EnumerateArray().Select(v => v.GetString()!).ToList();
        }
        return Array.Empty<string>();
    }

    [Test]
    public void IlVocabolarioChiusoDiceEsattamenteQuelloCheLaTabellaContiene()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "content", "amnesia", "declarations.json");
        var live = DeclarationTable.Load(path);
        Assert.That(live.IsOk, Is.True, live.Message);
        Assert.That(EnumDelCatalogo(), Is.EquivalentTo(live.Value!.Ids),
            "catalogo e dati sono andati alla deriva: una dichiarazione che il modello non puo' scegliere non verra' mai detta da nessuno");
    }

    [Test]
    public void IlVocabolarioNonEVuoto()
    {
        Assert.That(EnumDelCatalogo(), Is.Not.Empty, "senza enum il vocabolario non e' piu' chiuso");
    }
}
