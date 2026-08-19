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
        Function("end_conversation", "Chiude la conversazione dal lato del personaggio.", new JsonSchema
        {
            Properties = new Dictionary<string, JsonSchema>
            {
                ["reason"] = new() { Type = "string" },
            },
            Required = new[] { "reason" },
        }),
        // Il vocabolario e' chiuso: il modello non scrive un fatto, sceglie un id.
        // La prosa resta sua, solo l'id ha effetto sul mondo — ed e' l'esecutore a
        // decidere se quel personaggio, adesso, quella cosa ce l'ha davvero.
        // L'enum va tenuto allineato a content/amnesia/declarations.json.
        Function("dichiaro", "Segnala che il tuo personaggio ha appena detto una di queste cose. Scegli l'identificativo che corrisponde a cio' che hai detto; se non ne corrisponde nessuno, non chiamarlo.", new JsonSchema
        {
            Properties = new Dictionary<string, JsonSchema>
            {
                ["id"] = new()
                {
                    Type = "string",
                    EnumValues = new[] { "circolo_esisteva", "scampagnate", "non_erano_gite" },
                },
            },
            Required = new[] { "id" },
        }),
        // Gli strumenti nuovi vanno QUI, in fondo: il blocco e' un prefisso messo
        // in cache, e inserirne uno in mezzo invalida ogni conversazione (design §13).
    };

    public static IReadOnlyList<ToolDefinition> Schemas() => Catalog;

    private static ToolDefinition Function(string name, string description, JsonSchema parameters) =>
        new() { Function = new FunctionDefinition { Name = name, Description = description, Parameters = parameters } };
}
