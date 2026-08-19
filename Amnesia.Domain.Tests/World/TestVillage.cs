using Amnesia.Core;
using Amnesia.World;
using NUnit.Framework;

namespace Amnesia.Tests.World;

/// Il paese dei test non e' il paese del gioco: la geografia vera e' contenuto e
/// cambia con la storia, mentre queste sono le forme minime che servono a mettere
/// alla prova le regole — due stanze che condividono una porta, una bottega
/// dall'altra parte della strada, e un pezzo di aperto dove ci sono molte vie
/// ugualmente corte fra due celle.
internal static class TestVillage
{
    public static string FixturePath(string fileName) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "World", "fixtures", fileName);

    public static VillageMap Map()
    {
        var loaded = VillageMap.Load(FixturePath("village_map.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    public static RoutineTable Routines()
    {
        var loaded = RoutineTable.Load(FixturePath("routines.json"));
        Assert.That(loaded.IsOk, Is.True, loaded.Message);
        return loaded.Value!;
    }

    /// Un mondo con tutti al loro posto: Anna in cucina, Matteo in sala, Bruno in
    /// bottega, il giocatore in mezzo alla strada.
    public static WorldState World(VillageMap map)
    {
        var world = new WorldState();
        world.ActorOf("system");
        Presence.SetPosition(world, "player", Center(map, "strada"));
        Presence.SetPosition(world, "anna", Center(map, "cucina"));
        Presence.SetPosition(world, "matteo", Center(map, "sala"));
        Presence.SetPosition(world, "bruno", Center(map, "bottega"));
        return world;
    }

    public static Cell Center(VillageMap map, string place)
    {
        var center = map.CenterOf(place);
        Assert.That(center, Is.Not.Null, $"il luogo {place} esiste");
        return center!.Value;
    }

    public static Cell PositionOf(WorldState world, string actorId)
    {
        var cell = Presence.PositionOf(world, actorId);
        Assert.That(cell, Is.Not.Null, $"{actorId} ha una posizione");
        return cell!.Value;
    }
}
