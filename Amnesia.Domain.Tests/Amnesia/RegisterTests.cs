using Amnesia.Core;
using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

public class RegisterTests
{
    [Test]
    public void SuUnMondoVuotoNonECStabilitoNiente()
    {
        Assert.That(new Register(new WorldState()).IsEstablished("circolo_esisteva"), Is.False);
        Assert.That(new Register(new WorldState()).SupportsFor("circolo_esisteva"), Is.Empty);
    }

    [Test]
    public void UnaBoccaSolaNonStabilisceNiente()
    {
        var world = new WorldState();
        var register = new Register(world);

        register.Record("rosa", "circolo_esisteva");

        Assert.That(register.SupportsFor("circolo_esisteva"), Is.EqualTo(new[] { "rosa" }), "un sostegno e' registrato");
        Assert.That(register.IsEstablished("circolo_esisteva"), Is.False);
    }

    [Test]
    public void LaStessaBoccaDueVolteRestaUnaBoccaSola()
    {
        // Due sostegni vuol dire due persone, altrimenti chiunque si autoconferma
        // ripetendosi.
        var register = new Register(new WorldState());

        register.Record("rosa", "circolo_esisteva");
        register.Record("rosa", "circolo_esisteva");

        Assert.That(register.SupportsFor("circolo_esisteva"), Is.EqualTo(new[] { "rosa" }));
        Assert.That(register.IsEstablished("circolo_esisteva"), Is.False);
        Assert.That(register.TimesSaid("rosa", "circolo_esisteva"), Is.EqualTo(2), "le ripetizioni si contano lo stesso");
    }

    [Test]
    public void DueSostegniIndipendentiStabiliscono()
    {
        var register = new Register(new WorldState());

        register.Record("rosa", "circolo_esisteva");
        register.Record("matteo", "circolo_esisteva");

        Assert.That(register.SupportsFor("circolo_esisteva"),
            Is.EqualTo(new[] { "rosa", "matteo" }), "due bocche, in ordine di arrivo");
        Assert.That(register.IsEstablished("circolo_esisteva"), Is.True);
    }

    [Test]
    public void SiPuoChiedereCosaHaDettoUnPersonaggio()
    {
        var register = new Register(new WorldState());
        register.Record("rosa", "circolo_esisteva");
        register.Record("matteo", "scampagnate");
        register.Record("matteo", "circolo_esisteva");

        Assert.That(register.SaidBy("matteo"),
            Is.EqualTo(new[] { "circolo_esisteva", "scampagnate" }), "in ordine di arrivo nel registro");
        Assert.That(register.SaidBy("nessuno"), Is.Empty);
    }

    [Test]
    public void UnaDomandaAlRegistroNonSporcaIlMondo()
    {
        // Leggere non e' scrivere: mille id sbagliati letti non sono mille
        // dichiarazioni vuote dentro il salvataggio.
        var world = new WorldState();

        new Register(world).IsEstablished("id_inventato");
        new Register(world).TimesSaid("rosa", "id_inventato");

        Assert.That(world.Declarations, Is.Empty);
    }

    [Test]
    public void IlRegistroSopravviveAlGiroSulDisco()
    {
        // Il registro e' stato del mondo, non un campo di un servizio.
        var world = new WorldState();
        var register = new Register(world);
        register.Record("rosa", "circolo_esisteva");
        register.Record("rosa", "circolo_esisteva");
        register.Record("matteo", "circolo_esisteva");

        var reloaded = new Register(WorldState.FromJson(world.ToJson()));

        Assert.That(reloaded.IsEstablished("circolo_esisteva"), Is.True);
        Assert.That(reloaded.TimesSaid("rosa", "circolo_esisteva"), Is.EqualTo(2), "e con esso il conteggio delle ripetizioni");
        Assert.That(reloaded.SupportsFor("circolo_esisteva"), Is.EqualTo(new[] { "rosa", "matteo" }));
    }

    [Test]
    public void DueRegistriSulloStessoMondoVedonoLaStessaCosa()
    {
        var world = new WorldState();
        new Register(world).Record("rosa", "circolo_esisteva");
        new Register(world).Record("matteo", "circolo_esisteva");

        Assert.That(new Register(world).IsEstablished("circolo_esisteva"), Is.True,
            "lo stato sta nel mondo, mai nell'istanza");
    }
}
