namespace Amnesia.Core;

/// Una cosa accaduta nel mondo, con l'ora in cui e' accaduta. Il log degli eventi
/// e' la sola narrazione autorevole di una partita: la telemetria e i test ci
/// leggono dentro, e nessun testo generato puo' contraddirlo.
public sealed record WorldEvent(string Type, string ActorId, int Minute, IReadOnlyDictionary<string, string> Payload)
{
    public static WorldEvent Create(string type, string actorId, int minute, params (string Key, string Value)[] payload) =>
        new(type, actorId, minute, payload.ToDictionary(p => p.Key, p => p.Value));
}
