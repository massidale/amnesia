using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Amnesia.Core;

namespace Amnesia.Llm;

/// L'unica implementazione che parla davvero con OpenRouter.
public sealed class OpenRouterTransport : IChatTransport
{
    public const string Url = "https://openrouter.ai/api/v1/chat/completions";

    private const int ErrorBodyLimit = 300;

    /// Un HttpClient per processo, non uno per richiesta. Ogni istanza tiene il
    /// proprio pool di connessioni e le socket restano in TIME_WAIT ben oltre la
    /// Dispose: crearne una a ogni battuta di dialogo esaurisce le porte
    /// effimere nel giro di una conversazione lunga.
    private static readonly HttpClient SharedClient = new();

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly TimeSpan _timeout;

    public OpenRouterTransport(HttpClient http, string apiKey, TimeSpan timeout)
    {
        _http = http;
        _apiKey = apiKey;
        _timeout = timeout;
    }

    public OpenRouterTransport(HttpClient http, string apiKey, int timeoutSec)
        : this(http, apiKey, TimeSpan.FromSeconds(timeoutSec))
    {
    }

    public OpenRouterTransport(string apiKey, int timeoutSec)
        : this(SharedClient, apiKey, timeoutSec)
    {
    }

    public async Task<Result<LlmReply>> ChatAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        var body = OpenRouterCodec.SerializeRequest(OpenRouterCodec.BuildRequest(model, messages, tools));

        // La scadenza sta sulla singola richiesta e non su HttpClient.Timeout,
        // che e' una proprieta' del client condiviso e varrebbe per tutti.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            // La chiave viaggia sulla singola richiesta, non nei
            // DefaultRequestHeaders: il client e' condiviso, e una chiave piantata
            // li' resterebbe addosso a ogni altra chiamata fatta con lo stesso.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await _http.SendAsync(request, deadline.Token).ConfigureAwait(false);
            var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // Nel messaggio entra solo il corpo della risposta. Mai la
                // richiesta: porta l'header Authorization, e un errore finisce
                // nei log e negli screenshot.
                return Result<LlmReply>.Fail(
                    "api_error",
                    $"status {(int)response.StatusCode}: {Truncate(text, ErrorBodyLimit)}");
            }

            return OpenRouterCodec.ParseResponse(text);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Un provider che non risponde e' una condizione normale della
            // partita, non un errore di programma: esce come Result e chi chiama
            // decide se ripiegare sul modello di riserva.
            var seconds = _timeout.TotalSeconds.ToString("0.##", CultureInfo.InvariantCulture);
            return Result<LlmReply>.Fail("timeout", $"nessuna risposta entro {seconds} secondi");
        }
        catch (HttpRequestException error)
        {
            // Idem per la rete che cade: il messaggio dell'eccezione parla di
            // host e connessione, non della richiesta che abbiamo scritto noi.
            return Result<LlmReply>.Fail("transport_error", error.Message);
        }
    }

    private static string Truncate(string text, int limit) =>
        text.Length <= limit ? text : text.Substring(0, limit);
}
