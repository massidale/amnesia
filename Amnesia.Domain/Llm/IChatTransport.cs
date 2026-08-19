using Amnesia.Core;

namespace Amnesia.Llm;

/// Il confine fra il gioco e la rete. Esiste perche' i test devono poter
/// sostituire il provider con una risposta scritta a mano: nessun test di questo
/// codice apre una socket, mai.
public interface IChatTransport
{
    Task<Result<LlmReply>> ChatAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken cancellationToken = default);
}
