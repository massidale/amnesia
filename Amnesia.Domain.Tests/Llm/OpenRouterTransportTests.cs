using System.Net;
using System.Net.Http;
using Amnesia.Core;
using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Llm;

/// Nessuno di questi test apre una socket: l'HttpClient viene costruito su un
/// HttpMessageHandler finto, che risponde in memoria senza uscire dal processo.
public class OpenRouterTransportTests
{
    private const string FakeKey = "sk-or-fixture-non-reale";

    private const string GoodResponse = """
{"choices":[{"message":{"content":"Buongiorno."},"finish_reason":"stop"}],"usage":{"prompt_tokens":10,"completion_tokens":2}}
""";

    private static Task<Result<LlmReply>> Chat(OpenRouterTransport transport) =>
        transport.ChatAsync("m", new[] { ChatMessage.User("ciao") }, ToolCatalog.Schemas());

    [Test]
    public async Task UnaRispostaBuonaArrivaGiaDecodificata()
    {
        var handler = StubHandler.Returning(HttpStatusCode.OK, GoodResponse);
        using var client = new HttpClient(handler);

        var result = await Chat(new OpenRouterTransport(client, FakeKey, TimeSpan.FromSeconds(5)));

        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Value!.Text, Is.EqualTo("Buongiorno."));
        Assert.That(handler.LastRequestUri, Is.EqualTo(OpenRouterTransport.Url));
    }

    [Test]
    public async Task UnProviderCheRispondeMaleEUnFallimentoNonUnEccezione()
    {
        var handler = StubHandler.Returning(HttpStatusCode.InternalServerError, "il provider e' giu'");
        using var client = new HttpClient(handler);

        var result = await Chat(new OpenRouterTransport(client, FakeKey, TimeSpan.FromSeconds(5)));

        Assert.That(result.IsOk, Is.False);
        // Un 5xx ha un codice suo: e' l'unico caso in cui riprovare ha senso.
        Assert.That(result.Code, Is.EqualTo("server_error"));
        Assert.That(result.Message, Does.Contain("500").And.Contain("il provider e' giu'"));
        Assert.That(handler.Chiamate, Is.EqualTo(2), "una battuta persa si riprova una volta sola");
    }

    [Test]
    public async Task UnaRichiestaSbagliataNonSiRiprova()
    {
        var handler = StubHandler.Returning(HttpStatusCode.BadRequest, "modello inesistente");
        using var client = new HttpClient(handler);

        var result = await Chat(new OpenRouterTransport(client, FakeKey, TimeSpan.FromSeconds(5)));

        Assert.That(result.Code, Is.EqualTo("api_error"));
        Assert.That(handler.Chiamate, Is.EqualTo(1), "riprovare una richiesta malformata la sbaglia due volte");
    }

    [Test]
    public async Task UnCorpoDErroreEnormeVieneTroncato()
    {
        var handler = StubHandler.Returning(HttpStatusCode.BadRequest, new string('x', 5000));
        using var client = new HttpClient(handler);

        var result = await Chat(new OpenRouterTransport(client, FakeKey, TimeSpan.FromSeconds(5)));

        Assert.That(result.Message.Length, Is.LessThan(400), "un errore non trascina mezza risposta nei log");
    }

    [Test]
    public async Task LaReteCheCadeEUnFallimentoNonUnEccezione()
    {
        var handler = StubHandler.Throwing(new HttpRequestException("nome host non risolto"));
        using var client = new HttpClient(handler);

        var result = await Chat(new OpenRouterTransport(client, FakeKey, TimeSpan.FromSeconds(5)));

        Assert.That(result.IsOk, Is.False);
        Assert.That(result.Code, Is.EqualTo("transport_error"));
    }

    [Test]
    public async Task UnProviderCheNonRispondeScadeSenzaLanciare()
    {
        var handler = StubHandler.Sleeping(TimeSpan.FromSeconds(30));
        using var client = new HttpClient(handler);

        var result = await Chat(new OpenRouterTransport(client, FakeKey, TimeSpan.FromMilliseconds(30)));

        Assert.That(result.IsOk, Is.False);
        Assert.That(result.Code, Is.EqualTo("timeout"), "un provider lento e' una condizione normale, non un errore di programma");
    }

    [Test]
    public async Task LaChiaveViaggiaSullaRichiestaMaNonNeiMessaggiDErrore()
    {
        var handler = StubHandler.Returning(HttpStatusCode.Unauthorized, "no");
        using var client = new HttpClient(handler);

        var result = await Chat(new OpenRouterTransport(client, FakeKey, TimeSpan.FromSeconds(5)));

        Assert.That(handler.LastAuthorization, Is.EqualTo("Bearer " + FakeKey), "la chiave parte, altrimenti non si parla con nessuno");
        Assert.That(result.Message, Does.Not.Contain(FakeKey), "ma non torna indietro dentro un errore");
        Assert.That(client.DefaultRequestHeaders.Authorization, Is.Null, "e non resta appiccicata al client condiviso");
    }

    [Test]
    public async Task IlCorpoInviatoContieneModelloStrumentiEConversazione()
    {
        var handler = StubHandler.Returning(HttpStatusCode.OK, GoodResponse);
        using var client = new HttpClient(handler);

        await new OpenRouterTransport(client, FakeKey, TimeSpan.FromSeconds(5))
            .ChatAsync("deepseek/deepseek-v3.2", new[] { ChatMessage.User("ciao") }, ToolCatalog.Schemas());

        Assert.That(handler.LastBody, Does.Contain("\"model\":\"deepseek/deepseek-v3.2\""));
        Assert.That(handler.LastBody, Does.Contain("\"tool_choice\":\"auto\""));
        Assert.That(handler.LastBody, Does.Contain("record_claim"));
        Assert.That(handler.LastBody, Does.Not.Contain(FakeKey), "la chiave sta nell'header, non nel corpo");
    }

    [Test]
    public async Task IlFintoTrasportoStaAlPostoDelProvider()
    {
        IChatTransport transport = new FakeChatTransport()
            .Answers(Result<LlmReply>.Ok(new LlmReply { Text = "risposta scritta a mano" }));

        var result = await transport.ChatAsync("m", new[] { ChatMessage.User("ciao") }, ToolCatalog.Schemas());

        Assert.That(result.Value!.Text, Is.EqualTo("risposta scritta a mano"));
    }

    /// Risponde in memoria. Sostituire l'handler invece dell'intero HttpClient
    /// lascia sotto test anche il codice che costruisce la richiesta.
    private sealed class StubHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string _body = "";
        private Exception? _error;
        private TimeSpan _delay = TimeSpan.Zero;

        public string? LastRequestUri { get; private set; }

        public string? LastAuthorization { get; private set; }

        public string? LastBody { get; private set; }

        public int Chiamate { get; private set; }

        public static StubHandler Returning(HttpStatusCode status, string body) =>
            new() { _status = status, _body = body };

        public static StubHandler Throwing(Exception error) => new() { _error = error };

        public static StubHandler Sleeping(TimeSpan delay) => new() { _delay = delay };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chiamate++;
            LastRequestUri = request.RequestUri?.ToString();
            LastAuthorization = request.Headers.Authorization?.ToString();
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (_error is not null)
            {
                throw _error;
            }

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            }

            return new HttpResponseMessage(_status) { Content = new StringContent(_body) };
        }
    }
}
