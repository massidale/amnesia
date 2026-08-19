using Amnesia.Core;
using Amnesia.Llm;

namespace Amnesia.Tests.Llm;

/// Il provider sostituito da risposte scritte a mano. Esiste perche' nessun test
/// di questo progetto puo' toccare la rete: una suite che chiama un provider e'
/// lenta, costosa e rossa quando il provider ha una brutta giornata.
public sealed class FakeChatTransport : IChatTransport
{
    private readonly Queue<Result<LlmReply>> _answers = new();

    public List<string> SeenModels { get; } = new();

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = Array.Empty<ChatMessage>();

    public IReadOnlyList<ToolDefinition> LastTools { get; private set; } = Array.Empty<ToolDefinition>();

    public FakeChatTransport Answers(Result<LlmReply> answer)
    {
        _answers.Enqueue(answer);
        return this;
    }

    public Task<Result<LlmReply>> ChatAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        SeenModels.Add(model);
        LastMessages = messages;
        LastTools = tools;
        var answer = _answers.Count > 0
            ? _answers.Dequeue()
            : Result<LlmReply>.Fail("transport_error", "il finto trasporto non ha altre risposte pronte");
        return Task.FromResult(answer);
    }
}
