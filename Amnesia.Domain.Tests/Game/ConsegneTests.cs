using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Game;
using Amnesia.Llm;
using Amnesia.Tests.Llm;

namespace Amnesia.Tests.Game;

public class ConsegneTests
{
    private static ConversationSession Session(FakeChatTransport transport, WorldState world)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "content", "amnesia");
        var declarations = DeclarationTable.Load(Path.Combine(path, "declarations.json"));
        var positions = PositionTable.Load(Path.Combine(path, "positions.json"));
        Assert.That(declarations.IsOk, Is.True, declarations.Message);
        Assert.That(positions.IsOk, Is.True, positions.Message);
        var items = new ItemCatalog(new[] {
            new ItemDefinition("giacca", "una giacca", "la giacca"),
            new ItemDefinition("due_righe_matteo", "un biglietto firmato", "le due righe di Matteo")
        });
        var context = new ContextBuilder("regole", new Dictionary<string, string>
        {
            ["nino"] = "Nino", ["matteo"] = "Matteo"
        }, items, false, declarations.Value!, positions.Value!);
        return new ConversationSession(world, new ConversationLog(), context, transport,
            new DeclarationService(declarations.Value!, positions.Value!), items, "test");
    }

    [Test]
    public async Task IlSoloSalvataggioNonFaConsegnareLaGiacca()
    {
        var world = new WorldState();
        new Register(world).Record("matteo", "matteo_la_porto_via", true);
        var session = Session(FakeAnswers.Replying("Non ho altro."), world);
        var turn = await session.TakeTurnAsync("nino", "Hai qualcosa?");
        Assert.That(turn.IsOk, Is.True);
        Assert.That(turn.Received, Is.Empty);
        Assert.That(session.World.ItemOwners.ContainsKey("giacca"), Is.False);
    }

    [Test]
    public async Task DopoElenaVivaNinoConsegnaUnaSolaVolta()
    {
        var world = new WorldState();
        new Register(world).Record("matteo", "elena_viva", true);
        var transport = FakeAnswers.Replying("Ho trovato questa giacca, tienila.")
            .Answers(Result<LlmReply>.Ok(new LlmReply { Text = "Te l'ho già data." }));
        var session = Session(transport, world);
        var first = await session.TakeTurnAsync("nino", "Cosa hai trovato?");
        var second = await session.TakeTurnAsync("nino", "Hai altro?");
        Assert.That(first.IsOk && second.IsOk, Is.True);
        Assert.That(first.Received, Is.EqualTo(new[] { "giacca" }));
        Assert.That(second.Received, Is.Empty);
        Assert.That(session.World.ItemOwners["giacca"], Is.EqualTo("player"));
    }

    [Test]
    public async Task UnTurnoFallitoNonConsegnaOggetti()
    {
        var world = new WorldState();
        new Register(world).Record("matteo", "elena_viva", true);
        var session = Session(FakeAnswers.Failing("timeout", "nessuna risposta"), world);
        var turn = await session.TakeTurnAsync("nino", "Cosa hai trovato?");
        Assert.That(turn.IsOk, Is.False);
        Assert.That(turn.Received, Is.Empty);
        Assert.That(session.World.ItemOwners.ContainsKey("giacca"), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task LaLetteraRichiedeLaConfessioneAncheConLaGiacca(bool confessa)
    {
        var world = new WorldState();
        world.MarkShown("matteo", "quaderno_vittorio");
        new Register(world).Record("nino", "nino_giacca", true);
        world.ItemOwners["giacca"] = "player";
        var transport = confessa
            ? FakeAnswers.Calling("dichiaro", """{"id":"matteo_confessa"}""", "Sono stato io a colpirti. Ti scrivo due righe.")
            : FakeAnswers.Replying("La giacca era mia.");
        var session = Session(transport, world);
        var turn = await session.TakeTurnAsync("matteo", confessa ? "Sei stato tu a colpirmi?" : "Era tua?");
        Assert.That(turn.IsOk, Is.True);
        Assert.That(turn.Received.Contains("due_righe_matteo"), Is.EqualTo(confessa));
        Assert.That(session.World.ItemOwners.TryGetValue("due_righe_matteo", out var owner) && owner == "player", Is.EqualTo(confessa));
    }
}
