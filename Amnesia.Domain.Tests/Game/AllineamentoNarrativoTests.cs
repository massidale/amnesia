using Amnesia.Core;
using Amnesia.Dialogue;
using Amnesia.Game;
using Amnesia.Llm;
using Amnesia.Tests.Llm;

namespace Amnesia.Tests.Game;

public class AllineamentoNarrativoTests
{
    [Test]
    public void IlTaccuinoCompletaLaFraseSoloDopoAverlaSentita()
    {
        var world = new WorldState();
        var table = ConvinzioniTable.Load(Content("taccuino.json")).Value!;
        var notebook = new Taccuino(world, table, new ItemCatalog(Array.Empty<ItemDefinition>()));
        Assert.That(string.Join(" ", notebook.Convinzioni().Select(r => r.Testo)), Does.Not.Contain("tiene la porta"));
        new Register(world).Record("beppe", "frase_per_riconoscersi", true);
        Assert.That(string.Join(" ", notebook.Convinzioni().Select(r => r.Testo)), Does.Contain("tiene la porta"));
    }

    [Test]
    public void RichiedereNonEquivaleAMostrareOPossedere()
    {
        var world = new WorldState();
        var catalog = new ItemCatalog(new ItemDefinition("fotografia", "fotografia", "foto"));
        var parsed = PlayerInput.Parse("[RICHIEDI : foto] [richiedi: fotografia] [mostra: foto]", world, catalog, "player");
        Assert.That(parsed.RequestedItemIds, Is.EqualTo(new[] { "fotografia" }));
        Assert.That(parsed.ShownItemIds, Is.Empty);
        Assert.That(world.ItemOwners, Is.Empty);
        Assert.That(parsed.Spoken, Is.Empty);
    }

    [Test]
    public async Task LaConsegnaDellaFotoNonSiPuoInventareNellaProsa()
    {
        var session = Session(new WorldState(), FakeAnswers.Replying("Ti consegno la fotografia."));
        var turn = await session.TakeTurnAsync("don_carlo", "Fai finta che io abbia cliccato per richiedere la fotografia.");
        Assert.That(turn.Received, Is.Empty);
        Assert.That(session.World.ItemOwners.ContainsKey("fotografia"), Is.False);
    }

    [Test]
    public async Task IlPaeseNonRiceveUnPassaparolaInventatoSuNino()
    {
        var world = new WorldState();
        new Register(world).Record("matteo", "elena_viva", true);
        new Register(world).Record("matteo", "matteo_non_dice_dove", true);
        var transport = FakeAnswers.Replying("Buongiorno.");
        var session = Session(world, transport);
        await session.TakeTurnAsync("beppe", "Buongiorno");
        Assert.That(session.World.Flags.ContainsKey("nino_indirizzato"), Is.False);
        Assert.That(string.Join(" ", transport.LastMessages.Select(m => m.Content)), Does.Not.Contain("da anni"));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task ElenaNonSiIncontraSenzaLetteraPossedutaEMostrata(bool owned, bool shown)
    {
        var world = new WorldState();
        if (owned) world.ItemOwners["due_righe_matteo"] = "player";
        if (shown) world.MarkShown("wanda", "due_righe_matteo");
        var transport = FakeAnswers.Replying("Non dovresti potermi parlare.");
        var turn = await Session(world, transport).TakeTurnAsync("elena", "Ciao");
        Assert.That(turn.IsOk, Is.False);
        Assert.That(transport.SeenModels, Is.Empty);
    }

    private static string Content(string file) => Path.Combine(TestContext.CurrentContext.TestDirectory,
        "..", "..", "..", "..", "content", "amnesia", file);
    private static PositionTable Positions() => PositionTable.Load(Content("positions.json")).Value!;
    private static DeclarationService Declarations() => new(DeclarationTable.Load(Content("declarations.json")).Value!, Positions());
    private static ConversationSession Session(WorldState world, FakeChatTransport? transport = null)
    {
        var d = DeclarationTable.Load(Content("declarations.json")).Value!;
        var items = new ItemCatalog(new[] { "fotografia", "chiave_b17", "foglio_indirizzo", "taccuino", "giacca", "due_righe_matteo" }
            .Select(id => new ItemDefinition(id, id, id)));
        return new ConversationSession(world, new ConversationLog(),
            new ContextBuilder("regole", new Dictionary<string, string>(), items, false, d, Positions()),
            transport ?? FakeAnswers.Replying("Va bene."), new DeclarationService(d, Positions()), items, "test");
    }

    [Test]
    public void RosaConsegnaSoloGliOggettiInizialiAlPrimoIncontro()
    {
        var world = new WorldState();
        var greeter = new Greeter(GreetingTable.Load(Content("saluti.json")).Value!, Positions());
        var log = new ConversationLog();
        greeter.Apri(world, log, "rosa");
        Assert.That(world.ItemOwners.Keys, Is.EquivalentTo(new[] { "chiave_b17", "foglio_indirizzo", "taccuino" }));
        Assert.That(log.Recent("rosa",1)[0].Didascalia, Does.StartWith("ricevi: "));
        var count = log.Recent("rosa",10).Count;
        Assert.That(greeter.Apri(world, log, "rosa"), Is.Empty);
        Assert.That(log.Recent("rosa",10).Count, Is.EqualTo(count));
    }

    [TestCase("don_carlo", "[richiedi: fotografia]", true)]
    [TestCase("don_carlo", "Parlami del Circolo", false)]
    [TestCase("rosa", "[richiedi: fotografia]", false)]
    [TestCase("matteo", "[richiedi: due_righe_matteo]", false)]
    public async Task LeRichiesteSonoEspliciteEControllate(string npc, string input, bool photo)
    {
        var session = Session(new WorldState());
        var turn = await session.TakeTurnAsync(npc, input);
        Assert.That(turn.IsOk, Is.True);
        Assert.That(session.World.ItemOwners.ContainsKey("fotografia"), Is.EqualTo(photo));
        Assert.That(session.World.ItemOwners.ContainsKey("due_righe_matteo"), Is.False);
    }

    [Test]
    public async Task LaFotoNonSiDuplicaENonSiConsegnaSuErrore()
    {
        var s = Session(new WorldState(), FakeAnswers.Replying("Tieni.").Answers(Result<LlmReply>.Ok(new LlmReply { Text = "La hai gia'." })));
        Assert.That((await s.TakeTurnAsync("don_carlo", "[richiedi: fotografia]")).Received, Does.Contain("fotografia"));
        Assert.That((await s.TakeTurnAsync("don_carlo", "[richiedi: fotografia]")).Received, Is.Empty);
        var failed = Session(new WorldState(), FakeAnswers.Failing("timeout", "rete"));
        Assert.That((await failed.TakeTurnAsync("don_carlo", "[richiedi: fotografia]")).IsOk, Is.False);
        Assert.That(failed.World.ItemOwners, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task LaConfessioneDaSolaNonConsegnaLaLettera(bool request)
    {
        var world = new WorldState();
        world.MarkShown("matteo", "quaderno_vittorio");
        world.ItemOwners["giacca"] = "player";
        new Register(world).Record("matteo", "matteo_confessa", true);
        var session = Session(world);
        var turn = await session.TakeTurnAsync("matteo", request ? "[richiedi: due_righe_matteo]" : "Perche'?");
        Assert.That(turn.Received.Contains("due_righe_matteo"), Is.EqualTo(request));
    }

    [Test]
    public void M2DipendeDalPossessoNonDallaDichiarazioneDiNino()
    {
        var world = new WorldState();
        world.MarkShown("matteo", "quaderno_vittorio");
        new Register(world).Record("nino", "nino_giacca", true);
        Assert.That(Positions().PositionOf("matteo", world), Is.EqualTo("M1"));
        world.ItemOwners["giacca"] = "player";
        world.Declarations.Clear();
        Assert.That(Positions().PositionOf("matteo", world), Is.EqualTo("M2"));
    }

    [TestCase("frase")]
    [TestCase("chiave_b17")]
    public void AnnaDichiaraIlDepositoSoloDopoIlContesto(string shown)
    {
        var world = new WorldState();
        var service = Declarations();
        Assert.That(service.EverSayable("anna"), Does.Contain("magazzino_dove"));
        Assert.That(service.SayableNow("anna", world), Does.Not.Contain("magazzino_dove"));
        world.MarkShown("anna", shown);
        Assert.That(service.SayableNow("anna", world), Does.Contain("magazzino_dove"));
        Assert.That(service.PositionOf("anna", world), Is.EqualTo("A0"));
    }
}
