using System.Globalization;
using System.Text;

namespace Amnesia;

/// Le cinque parole.
///
/// Non sono un oggetto e non si mostrano: stanno sulla prima pagina del
/// taccuino, e per usarle bisogna dirle. E' l'unica cosa del gioco che il
/// giocatore deve *fare* con le proprie mani invece che selezionare — ed e'
/// giusto cosi', perche' e' l'unica che Giorgio si porta dietro da ventun anni
/// senza sapere cosa sia.
///
/// Il riconoscimento non e' letterale al carattere. Chi la copia dal taccuino la
/// scrive giusta, ma chi la ricopia a memoria sbaglia un accento o si mangia un
/// articolo, e un gioco che per questo non reagisce sembra rotto. Si contano
/// invece le parole che portano il senso — passa, primo, tiene, porta — e ne
/// bastano tre su quattro: una parola sbagliata e' un errore di battitura, due
/// e' un'altra frase.
public static class Frase
{
    public const string Testo = "Chi passa per primo tiene la porta";

    /// Le parole che la fanno essere quella e non un'altra. Gli articoli e le
    /// preposizioni non contano: nessuno riconosce una formula dal «per».
    private static readonly string[] Portanti = { "passa", "primo", "tiene", "porta" };

    private const int Necessarie = 3;

    public static bool Detta(string parole)
    {
        if (string.IsNullOrWhiteSpace(parole))
        {
            return false;
        }
        var nudo = Nudo(parole);
        var trovate = 0;
        foreach (var parola in Portanti)
        {
            if (nudo.Contains(" " + parola))
            {
                trovate++;
            }
        }
        return trovate >= Necessarie;
    }

    /// Minuscolo, senza accenti e senza punteggiatura, con uno spazio davanti a
    /// ogni parola: cosi' «Porta.» e «porta» sono la stessa cosa, e «riportare»
    /// non lo e'.
    private static string Nudo(string testo)
    {
        var scomposto = testo.Normalize(NormalizationForm.FormD);
        var pulito = new StringBuilder(" ");
        var spazio = true;
        foreach (var carattere in scomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(carattere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            if (char.IsLetterOrDigit(carattere))
            {
                pulito.Append(char.ToLowerInvariant(carattere));
                spazio = false;
            }
            else if (!spazio)
            {
                pulito.Append(' ');
                spazio = true;
            }
        }
        return pulito.ToString();
    }
}
