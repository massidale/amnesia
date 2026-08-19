namespace Amnesia.Llm;

/// Gli strumenti che il personaggio puo' invocare.
///
/// Ordine e contenuto devono restare identici byte per byte per tutta la
/// partita: il blocco degli strumenti fa parte del prefisso del prompt che il
/// provider tiene in cache (design §13). Gli strumenti nuovi si aggiungono IN
/// FONDO — infilarne uno in mezzo invaliderebbe la cache di ogni conversazione
/// gia' aperta, e ogni turno successivo si ripagherebbe il prefisso intero.
public static class ToolCatalog
{
    /// Costruito una volta sola e restituito sempre lo stesso: cosi' l'identita'
    /// byte per byte non dipende dal fatto che due costruzioni successive si
    /// somiglino, ma dal fatto che siano la stessa cosa.
    private static readonly IReadOnlyList<ToolDefinition> Catalog = new[]
    {
        Function("record_claim", "Registra un'affermazione fattuale del giocatore, senza renderla vera.", new JsonSchema
        {
            Properties = new Dictionary<string, JsonSchema>
            {
                ["fact_id"] = new() { Type = "string", Description = "Identificatore breve in snake_case dell'affermazione." },
                ["content"] = new() { Type = "string" },
                ["confidence"] = new() { Type = "number", Description = "Quanto il personaggio ci crede.", Minimum = 0.0, Maximum = 1.0 },
            },
            Required = new[] { "fact_id", "content", "confidence" },
        }),
        Function("attempt_action", "Descrive un tentativo fisico del personaggio. Azioni non supportate restano impraticabili.", new JsonSchema
        {
            Properties = new Dictionary<string, JsonSchema>
            {
                ["action"] = new() { Type = "string" },
                ["target"] = new() { Type = "string" },
                ["means"] = new() { Type = "string" },
            },
            Required = new[] { "action", "target" },
        }),
        Function("resolve_signature_request", "SOLO per Giorgio, SOLO quando il giocatore chiede esplicitamente la firma. Il motore decide l'esito.", new JsonSchema
        {
            Properties = new Dictionary<string, JsonSchema>
            {
                ["perceived_request"] = new()
                {
                    Type = "string",
                    Description = "Cosa Giorgio crede di firmare.",
                    EnumValues = new[] { "deposition", "innocuous_paper" },
                },
            },
            Required = new[] { "perceived_request" },
        }),
        Function("end_conversation", "Chiude la conversazione dal lato del personaggio.", new JsonSchema
        {
            Properties = new Dictionary<string, JsonSchema>
            {
                ["reason"] = new() { Type = "string" },
            },
            Required = new[] { "reason" },
        }),
        // Gli strumenti nuovi vanno QUI, in fondo: il blocco e' un prefisso messo
        // in cache, e inserirne uno in mezzo invalida ogni conversazione (design §13).
    };

    public static IReadOnlyList<ToolDefinition> Schemas() => Catalog;

    private static ToolDefinition Function(string name, string description, JsonSchema parameters) =>
        new() { Function = new FunctionDefinition { Name = name, Description = description, Parameters = parameters } };
}
