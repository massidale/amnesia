namespace Amnesia.Game;

/// Cosa e' successo in un turno. Il chiamante — che sia un terminale, una scena
/// 3D o un test — non deve saper leggere lo stato del mondo per raccontarlo.
public sealed record TurnResult
{
    public bool IsOk { get; init; }

    /// Popolati solo quando il turno non e' avvenuto.
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";

    public string NpcId { get; init; } = "";

    /// Cio' che il personaggio ha detto.
    public string Reply { get; init; } = "";

    /// Le etichette che il giocatore ha scritto e che il motore ha rifiutato —
    /// un oggetto che non possiede, due righe che non ha raccolto. Vanno dette,
    /// non ingoiate: un tag che sparisce in silenzio si legge come un guasto.
    public IReadOnlyList<string> RefusedTags { get; init; } = Array.Empty<string>();

    /// Le dichiarazioni che il motore ha registrato in questo turno.
    public IReadOnlyList<string> Declared { get; init; } = Array.Empty<string>();

    /// Gli oggetti passati in mano al giocatore in questo turno: la consegna la
    /// decidono i dati della scala, mai il modello.
    public IReadOnlyList<string> Received { get; init; } = Array.Empty<string>();

    /// Quelle che il personaggio ha provato a fare e che non poteva fare: un
    /// modello puo' sempre scegliere l'identificativo sbagliato, e la decisione
    /// su cosa risulti detto non e' mai sua.
    public IReadOnlyList<string> RefusedDeclarations { get; init; } = Array.Empty<string>();

    public int Minute { get; init; }

    /// Diagnostica del turno (perche' a volte lo schermo resta muto): come il
    /// provider ha chiuso la risposta, quanti token di completamento ha speso, e
    /// quanto lungo era il "pensiero" reasoning. Se il turno riesce ma Reply e'
    /// vuota con FinishReason "length" e ReasoningLength grande, il budget e'
    /// finito nel ragionamento e il testo non e' mai uscito.
    public string FinishReason { get; init; } = "";
    public int CompletionTokens { get; init; }
    public int ReasoningLength { get; init; }

    public static TurnResult Fail(string code, string message) =>
        new() { IsOk = false, Code = code, Message = message };
}
