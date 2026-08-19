using System.Linq;
using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Il vocabolario e' chiuso e la decisione non e' del modello: sceglie un id da
/// una lista, e il motore rifiuta gli id che non esistono e quelli che la
/// posizione di chi parla non gli concede.
public class DichiaroTests
{
    private static DeclarationService Service() =>
        new(TestDeclarations.Table(), TestDeclarations.Positions());

    [Test]
    public void IlCatalogoEsponeDichiaroConUnVocabolarioChiuso()
    {
        var dichiaro = ToolCatalog.Schemas().Single(tool => tool.Function.Name == "dichiaro");

        // Quali id ci siano dentro lo verifica CatalogoEDatiTests contro i dati:
        // qui si difende la forma, cioe' che l'id sia una scelta da una lista
        // chiusa e non una stringa che il modello riempie come vuole.
        Assert.That(dichiaro.Function.Parameters.Properties!["id"].EnumValues, Is.Not.Null.And.Not.Empty,
            "l'id e' un vocabolario chiuso, non testo libero");
        Assert.That(dichiaro.Function.Parameters.Properties!["id"].EnumValues,
            Is.SupersetOf(new[] { "circolo_esisteva", "scampagnate", "non_erano_gite" }));
        Assert.That(dichiaro.Function.Parameters.Required, Is.EqualTo(new[] { "id" }));
    }

    [Test]
    public void DichiaroELUltimoDelCatalogo()
    {
        // I nuovi strumenti vanno IN FONDO: il blocco e' un prefisso in cache.
        var names = ToolCatalog.Schemas().Select(tool => tool.Function.Name).ToArray();

        Assert.That(names[names.Length - 1], Is.EqualTo("dichiaro"));
    }

    [Test]
    public void SulPrimoGradinoNonPuoDichiarareUnaRigaCheNonHa()
    {
        var cold = TestDeclarations.World();

        var refused = Service().Declare(cold, "matteo", "non_erano_gite");

        Assert.That(refused.IsOk, Is.False);
        Assert.That(refused.Code, Is.EqualTo("declaration_not_granted"));
        Assert.That(new Register(cold).SupportsFor("non_erano_gite"), Is.Empty, "e il registro resta pulito");
    }

    [Test]
    public void SulGradinoDopoLaDichiarazionePassaEIlRegistroLaTiene()
    {
        var told = TestDeclarations.World("frase");

        var accepted = Service().Declare(told, "matteo", "non_erano_gite");

        Assert.That(accepted.IsOk, Is.True);
        Assert.That(new Register(told).SupportsFor("non_erano_gite"), Is.EqualTo(new[] { "matteo" }));
        Assert.That(accepted.Events.Single().Payload["declaration_id"], Is.EqualTo("non_erano_gite"),
            "e l'accaduto finisce nel log degli eventi, che e' la sola narrazione autorevole");
    }

    [Test]
    public void UnIdFuoriTabellaNonEntraMaiNelRegistro()
    {
        // Un modello puo' sempre scegliere l'id sbagliato, comunque gli arrivi.
        var told = TestDeclarations.World("frase");

        var junk = Service().Declare(told, "matteo", "id_inventato");

        Assert.That(junk.IsOk, Is.False);
        Assert.That(junk.Code, Is.EqualTo("unknown_declaration"));
        Assert.That(new Register(told).SupportsFor("id_inventato"), Is.Empty, "e non lascia traccia");
        Assert.That(told.Declarations, Is.Empty);
    }
}
