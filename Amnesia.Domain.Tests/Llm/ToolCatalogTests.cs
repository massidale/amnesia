using System.Linq;
using System.Text.Json;
using Amnesia.Llm;
using NUnit.Framework;

namespace Amnesia.Tests.Llm;

/// Il blocco degli strumenti e' il prefisso messo in cache dal provider: questi
/// test difendono la sua immobilita'. Se falliscono, la domanda giusta non e'
/// "come aggiorno il test" ma "posso permettermi di invalidare ogni cache".
public class ToolCatalogTests
{
    private const string Golden = """
[{"type":"function","function":{"name":"record_claim","description":"Registra un'affermazione fattuale del giocatore, senza renderla vera.","parameters":{"type":"object","properties":{"fact_id":{"type":"string","description":"Identificatore breve in snake_case dell'affermazione."},"content":{"type":"string"},"confidence":{"type":"number","description":"Quanto il personaggio ci crede.","minimum":0,"maximum":1}},"required":["fact_id","content","confidence"]}}},{"type":"function","function":{"name":"attempt_action","description":"Descrive un tentativo fisico del personaggio. Azioni non supportate restano impraticabili.","parameters":{"type":"object","properties":{"action":{"type":"string"},"target":{"type":"string"},"means":{"type":"string"}},"required":["action","target"]}}},{"type":"function","function":{"name":"end_conversation","description":"Chiude la conversazione dal lato del personaggio.","parameters":{"type":"object","properties":{"reason":{"type":"string"}},"required":["reason"]}}},{"type":"function","function":{"name":"dichiaro","description":"Segnala che il tuo personaggio ha appena detto una di queste cose. Scegli l'identificativo che corrisponde a cio' che hai detto; se non ne corrisponde nessuno, non chiamarlo.","parameters":{"type":"object","properties":{"id":{"type":"string","enum":["circolo_esisteva","scampagnate","avevano_una_frase","non_erano_gite","padre_nel_circolo","magazzino_dove","affitto","versione_paese","elena_figlia_vittorio","corpo_mai_trovato","il_rito","sacrificio_per_vittorio","elena_prescelta","laura_non_cera","laura_assoggettata","anna_solo_matteo","padre_nella_cava","io_ero_con_lui","matteo_ero_gia_sceso","matteo_la_porto_via","elena_viva","matteo_non_dice_dove","matteo_cosa_ti_ricordi","matteo_mai_parlati","matteo_confessa","usciva_allegro","segatura","referto","dove_lo_trovai","giovedi_sabato","nessuno_denuncio","padre_cercava","padre_veniva_da_me","busta_esiste","don_carlo_manda","don_carlo_consegna","wanda_una_persona_sola","elena_adottata","frase_per_riconoscersi","foto_anna_e_pietro","foto_matteo","foto_andrea","foto_vittorio","foto_laura","vittorio_morto","vittorio_capo","nino_indizio"]}},"required":["id"]}}}]
""";

    private static string Serialized() =>
        JsonSerializer.Serialize(ToolCatalog.Schemas(), OpenRouterCodec.SerializerOptions);

    [Test]
    public void LOrdineDelCatalogoEFissato()
    {
        var names = ToolCatalog.Schemas().Select(tool => tool.Function.Name).ToArray();

        Assert.That(names, Is.EqualTo(new[]
        {
            "record_claim",
            "attempt_action",
            "end_conversation",
            "dichiaro",
        }), "gli strumenti nuovi vanno in fondo, mai in mezzo");
    }

    [Test]
    public void IlBloccoEIdenticoAOgniChiamata()
    {
        var first = Serialized();
        var second = Serialized();
        var firstList = ToolCatalog.Schemas();
        var secondList = ToolCatalog.Schemas();

        Assert.That(second, Is.EqualTo(first));
        Assert.That(secondList, Is.SameAs(firstList), "e' proprio lo stesso oggetto, non una copia che gli somiglia");
    }

    /// La forma esatta del prefisso, byte per byte. Ogni carattere qui dentro e'
    /// gia' stato pagato una volta dal provider e messo in cache: cambiarne uno
    /// costa il prefisso intero, su ogni conversazione aperta.
    [Test]
    public void IlBloccoEQuestoEBasta()
    {
        Assert.That(Serialized(), Is.EqualTo(Golden));
    }

    [Test]
    public void GliStrumentiRitiratiNonTornano()
    {
        var names = ToolCatalog.Schemas().Select(tool => tool.Function.Name).ToArray();

        Assert.That(names, Does.Not.Contain("appraise_turn"), "il nuovo disegno non ha punteggi numerici");
        Assert.That(names, Does.Not.Contain("resolve_signature_request"),
            "la firma era la deposizione del gioco di prima: in Amnesia non si firma niente");
        Assert.That(names, Does.Not.Contain("accept_offer"));
        Assert.That(names, Does.Not.Contain("propose_terms"));
        Assert.That(names, Does.Not.Contain("respond_to_proposal"));
    }

    [Test]
    public void UnAffermazioneChiedeIdentificatoreContenutoEQuantoCiSiCrede()
    {
        var parameters = ToolCatalog.Schemas()[0].Function.Parameters;

        Assert.That(parameters.Required, Is.EqualTo(new[] { "fact_id", "content", "confidence" }));
        Assert.That(parameters.Properties!["confidence"].Minimum, Is.EqualTo(0.0));
        Assert.That(parameters.Properties!["confidence"].Maximum, Is.EqualTo(1.0));
    }

    [Test]
    public void UnTentativoFisicoChiedeAlmenoAzioneEBersaglio()
    {
        var parameters = ToolCatalog.Schemas()
            .Single(tool => tool.Function.Name == "attempt_action")
            .Function.Parameters;

        Assert.That(parameters.Required, Is.EqualTo(new[] { "action", "target" }));
        Assert.That(parameters.Properties!.ContainsKey("means"), Is.True, "il mezzo e' facoltativo ma va offerto");
    }
}
