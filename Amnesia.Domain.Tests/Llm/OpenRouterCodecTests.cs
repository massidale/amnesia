using System.Text.Json;
using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Llm;

/// Questi test fissano la forma sul filo. Uno scarto qui non rompe la
/// compilazione: fallisce contro il provider, a partita avviata, che e' il posto
/// peggiore per accorgersene.
public class OpenRouterCodecTests
{
    private const string ResponseWithTools = """
{"choices":[{"message":{"content":"Vediamo...","tool_calls":[{"id":"call_1","type":"function","function":{"name":"record_claim","arguments":"{\"fact_id\": \"debito_pagato\", \"confidence\": 0.7}"}}]},"finish_reason":"tool_calls"}],"usage":{"prompt_tokens":900,"completion_tokens":80}}
""";

    private const string ResponseTextOnly = """
{"choices":[{"message":{"content":"Buongiorno."},"finish_reason":"stop"}],"usage":{"prompt_tokens":500,"completion_tokens":12}}
""";

    private const string ResponseBadArgs = """
{"choices":[{"message":{"content":null,"tool_calls":[{"id":"call_2","type":"function","function":{"name":"record_claim","arguments":"{rotto"}}]},"finish_reason":"tool_calls"}],"usage":{}}
""";

    private static readonly ToolDefinition SampleTool = new()
    {
        Function = new FunctionDefinition
        {
            Name = "prova",
            Description = "descrizione",
            Parameters = new JsonSchema
            {
                Properties = new Dictionary<string, JsonSchema> { ["x"] = new() { Type = "string" } },
                Required = new[] { "x" },
            },
        },
    };

    [Test]
    public void IlCorpoDellaRichiestaHaEsattamenteLaFormaDelFilo()
    {
        var body = OpenRouterCodec.SerializeRequest(OpenRouterCodec.BuildRequest(
            "anthropic/claude-sonnet-4.5",
            new[] { ChatMessage.User("ciao") },
            new[] { SampleTool }));

        Assert.That(body, Is.EqualTo(
            """{"model":"anthropic/claude-sonnet-4.5","messages":[{"role":"user","content":"ciao"}],"tools":[{"type":"function","function":{"name":"prova","description":"descrizione","parameters":{"type":"object","properties":{"x":{"type":"string"}},"required":["x"]}}}],"tool_choice":"auto"}"""));
    }

    [Test]
    public void ILivelliDiMessaggioEscononoConICampiCheLiRiguardano()
    {
        var toolResult = JsonSerializer.Serialize(ChatMessage.ToolResult("call_1", "ok"), OpenRouterCodec.SerializerOptions);
        var user = JsonSerializer.Serialize(ChatMessage.User("ciao"), OpenRouterCodec.SerializerOptions);

        Assert.That(user, Is.EqualTo("""{"role":"user","content":"ciao"}"""), "niente campi nulli sul filo");
        Assert.That(toolResult, Is.EqualTo("""{"role":"tool","content":"ok","tool_call_id":"call_1"}"""));
    }

    [Test]
    public void UnaRispostaConChiamateAStrumentiSiDecodifica()
    {
        var parsed = OpenRouterCodec.ParseResponse(ResponseWithTools);

        Assert.That(parsed.IsOk, Is.True);
        var reply = parsed.Value!;
        Assert.That(reply.Text, Is.EqualTo("Vediamo..."));
        Assert.That(reply.FinishReason, Is.EqualTo("tool_calls"));
        Assert.That(reply.Usage.PromptTokens, Is.EqualTo(900));
        Assert.That(reply.Usage.CompletionTokens, Is.EqualTo(80));
        Assert.That(reply.ToolCalls.Count, Is.EqualTo(1));
        Assert.That(reply.ToolCalls[0].Id, Is.EqualTo("call_1"));
        Assert.That(reply.ToolCalls[0].Name, Is.EqualTo("record_claim"));
        Assert.That(reply.ToolCalls[0].Arguments.GetProperty("confidence").GetDouble(), Is.EqualTo(0.7));
        Assert.That(reply.ToolCalls[0].Arguments.GetProperty("fact_id").GetString(), Is.EqualTo("debito_pagato"));
    }

    [Test]
    public void UnaChiamataTornaSulFiloComeEArrivata()
    {
        var call = OpenRouterCodec.ParseResponse(ResponseWithTools).Value!.ToolCalls[0];

        var message = JsonSerializer.Serialize(
            ChatMessage.Assistant("Vediamo...", new[] { call.ToWire() }),
            OpenRouterCodec.SerializerOptions);

        Assert.That(message, Is.EqualTo(
            """{"role":"assistant","content":"Vediamo...","tool_calls":[{"id":"call_1","type":"function","function":{"name":"record_claim","arguments":"{\"fact_id\": \"debito_pagato\", \"confidence\": 0.7}"}}]}"""),
            "gli argomenti tornano indietro carattere per carattere");
    }

    [Test]
    public void UnaRispostaDiSoloTestoNonHaChiamate()
    {
        var parsed = OpenRouterCodec.ParseResponse(ResponseTextOnly);

        Assert.That(parsed.Value!.Text, Is.EqualTo("Buongiorno."));
        Assert.That(parsed.Value!.ToolCalls, Is.Empty);
        Assert.That(parsed.Value!.FinishReason, Is.EqualTo("stop"));
    }

    [Test]
    public void UnContenutoNulloDiventaTestoVuoto()
    {
        var parsed = OpenRouterCodec.ParseResponse("""{"choices":[{"message":{"content":null}}]}""");

        Assert.That(parsed.Value!.Text, Is.Empty);
    }

    [Test]
    public void SpazzaturaNonEUnaRisposta()
    {
        Assert.That(OpenRouterCodec.ParseResponse("spazzatura").Code, Is.EqualTo("malformed_response"));
        Assert.That(OpenRouterCodec.ParseResponse("{}").Code, Is.EqualTo("malformed_response"), "choices mancante");
        Assert.That(OpenRouterCodec.ParseResponse("""{"choices":[]}""").Code, Is.EqualTo("malformed_response"), "choices vuoto");
        Assert.That(OpenRouterCodec.ParseResponse("""{"choices":7}""").Code, Is.EqualTo("malformed_response"), "choices non e' un array");
        Assert.That(OpenRouterCodec.ParseResponse("""{"choices":["no"]}""").Code, Is.EqualTo("malformed_response"), "la scelta non e' un oggetto");
        Assert.That(OpenRouterCodec.ParseResponse("""{"choices":[{"message":{"tool_calls":"no"}}]}""").Code, Is.EqualTo("malformed_response"), "tool_calls non e' un array");
    }

    [Test]
    public void ArgomentiRottiFannoCadereTuttaLaRisposta()
    {
        var parsed = OpenRouterCodec.ParseResponse(ResponseBadArgs);

        Assert.That(parsed.IsOk, Is.False);
        Assert.That(parsed.Code, Is.EqualTo("malformed_tool_arguments"));
        Assert.That(parsed.Message, Is.EqualTo("record_claim"), "il messaggio dice quale strumento");
    }

    [Test]
    public void ArgomentiCheNonSonoUnOggettoValgonoComeRotti()
    {
        var asObject = OpenRouterCodec.ParseResponse(
            """{"choices":[{"message":{"tool_calls":[{"id":"c","function":{"name":"record_claim","arguments":{"fact_id":"x"}}}]}}]}""");
        var asScalar = OpenRouterCodec.ParseResponse(
            """{"choices":[{"message":{"tool_calls":[{"id":"c","function":{"name":"record_claim","arguments":"7"}}]}}]}""");

        Assert.That(asObject.Code, Is.EqualTo("malformed_tool_arguments"), "sul filo gli argomenti sono una stringa, non un oggetto");
        Assert.That(asScalar.Code, Is.EqualTo("malformed_tool_arguments"));
    }

    [Test]
    public void UnConteggioDiTokenStortoNonButtaViaLaRisposta()
    {
        var parsed = OpenRouterCodec.ParseResponse("""{"choices":[{"message":{"content":"ok"}}],"usage":7}""");

        Assert.That(parsed.IsOk, Is.True);
        Assert.That(parsed.Value!.Usage, Is.EqualTo(TokenUsage.Empty));
    }
}
