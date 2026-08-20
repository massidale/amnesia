using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Game;
using NUnit.Framework;

namespace Amnesia.Tests.Game;

public class TaccuinoTests
{
    private static ItemCatalog Catalogo() => new(
        new ItemDefinition("fotografia", "una fotografia sbiadita", "la fotografia"),
        new ItemDefinition("braccialetto", "un braccialetto d'argento", "il braccialetto"),
        new ItemDefinition("chiave_b17", "una chiave d'ottone", "la chiave"));

    private static ConvinzioniTable Convinzioni() => ConvinzioniTable.FromJson("""
    {"convinzioni": [
      {"id": "coma", "testo": "Sono stato due anni in coma.", "iniziale": true},
      {"id": "elena_morta", "testo": "Elena e' morta nella cava.", "quando": ["versione_paese"]},
      {"id": "elena_salvata", "testo": "Elena e' viva: l'ha portata via Matteo.",
       "quando": ["matteo_la_porto_via", "elena_viva"], "sostituisce": "elena_morta"}
    ]}
    """).Value!;

    [Test]
    public void LeTascheContengonoSoloCioCheIlGiocatoreHa()
    {
        var world = new WorldState();
        world.ItemOwners["fotografia"] = "player";
        world.ItemOwners["braccialetto"] = "matteo";

        var tasche = new Taccuino(world, Convinzioni(), Catalogo()).Tasche();

        Assert.That(tasche.Select(oggetto => oggetto.Id), Is.EqualTo(new[] { "fotografia" }));
    }

    [Test]
    public void LeTascheSeguonoLOrdineDelCatalogoENonQuelloDelDizionario()
    {
        var world = new WorldState();
        world.ItemOwners["chiave_b17"] = "player";
        world.ItemOwners["fotografia"] = "player";

        var tasche = new Taccuino(world, Convinzioni(), Catalogo()).Tasche();

        Assert.That(tasche.Select(oggetto => oggetto.Id), Is.EqualTo(new[] { "fotografia", "chiave_b17" }));
    }

    [Test]
    public void IlTaccuinoParteConLeSoleRigheIniziali()
    {
        var righe = new Taccuino(new WorldState(), Convinzioni(), Catalogo()).Convinzioni();

        Assert.That(righe.Select(riga => riga.Id), Is.EqualTo(new[] { "coma" }));
        Assert.That(righe[0].Cancellata, Is.False);
    }

    [Test]
    public void UnaConvinzioneCompareQuandoQualcunoLaDiceDavvero()
    {
        var world = new WorldState();
        new Register(world).Record("rosa", "versione_paese");

        var righe = new Taccuino(world, Convinzioni(), Catalogo()).Convinzioni();

        Assert.That(righe.Select(riga => riga.Id), Is.EqualTo(new[] { "coma", "elena_morta" }));
    }

    [Test]
    public void LaRigaSmentitaRestaCancellataAPenna()
    {
        var world = new WorldState();
        var registro = new Register(world);
        registro.Record("rosa", "versione_paese");
        registro.Record("matteo", "elena_viva");

        var righe = new Taccuino(world, Convinzioni(), Catalogo()).Convinzioni();

        Assert.That(righe.Select(riga => riga.Id), Is.EqualTo(new[] { "coma", "elena_morta", "elena_salvata" }));
        Assert.That(righe.Single(riga => riga.Id == "elena_morta").Cancellata, Is.True,
            "cancellata a penna, non strappata: i segni degli errori sono la trama che si muove");
        Assert.That(righe.Single(riga => riga.Id == "elena_salvata").Cancellata, Is.False);
    }

    [Test]
    public void UnaSmentitaSenzaLaRigaVecchiaNonCancellaNiente()
    {
        // Il giocatore che arriva a Matteo senza aver mai sentito la versione
        // del paese non ha niente da cancellare: compare solo la riga nuova.
        var world = new WorldState();
        new Register(world).Record("matteo", "matteo_la_porto_via");

        var righe = new Taccuino(world, Convinzioni(), Catalogo()).Convinzioni();

        Assert.That(righe.Select(riga => riga.Id), Is.EqualTo(new[] { "coma", "elena_salvata" }));
    }

    [Test]
    public void IlTaccuinoVeroSiCaricaECopreLeDichiarazioniVere()
    {
        var vera = ConvinzioniTable.Load(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..",
                "content", "amnesia", "taccuino.json"));
        Assert.That(vera.IsOk, Is.True, vera.Message);

        // Ogni id in "quando" deve esistere nella tabella delle dichiarazioni:
        // un refuso qui e' una riga che non comparira' mai.
        var dichiarazioni = Amnesia.Tests.Declarations.TestDeclarations.Table().Ids.ToHashSet();
        foreach (var riga in vera.Value!.Righe)
        {
            foreach (var id in riga.Quando)
            {
                Assert.That(dichiarazioni, Does.Contain(id), $"{riga.Id}: quando «{id}» non esiste");
            }
            if (riga.Sostituisce.Length > 0)
            {
                Assert.That(vera.Value.Righe.Select(r => r.Id), Does.Contain(riga.Sostituisce),
                    $"{riga.Id}: sostituisce una riga che non c'e'");
            }
        }
    }
}
