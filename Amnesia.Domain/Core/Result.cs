namespace Amnesia.Core;

/// Esito di un'operazione di dominio. Il motore non lancia eccezioni per le cose
/// che possono legittimamente andare storte — un giocatore che mostra un oggetto
/// che non ha non e' un errore di programma, e' una mossa rifiutata.
public sealed class Result
{
    public bool IsOk { get; private init; }
    public string Code { get; private init; } = "";
    public string Message { get; private init; } = "";
    public IReadOnlyList<WorldEvent> Events { get; private init; } = Array.Empty<WorldEvent>();

    public static Result Ok(params WorldEvent[] events) => new() { IsOk = true, Events = events };

    public static Result Fail(string code, string message) =>
        new() { IsOk = false, Code = code, Message = message };
}

/// La stessa cosa quando c'e' un valore da restituire.
public sealed class Result<T>
{
    public bool IsOk { get; private init; }
    public string Code { get; private init; } = "";
    public string Message { get; private init; } = "";
    public T? Value { get; private init; }

    public static Result<T> Ok(T value) => new() { IsOk = true, Value = value };

    public static Result<T> Fail(string code, string message) =>
        new() { IsOk = false, Code = code, Message = message };
}
