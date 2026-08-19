using NUnit.Framework;

namespace Amnesia.Tests.Declarations;

/// Il riconoscimento delle cinque parole. E' l'unica cosa che il giocatore deve
/// scrivere di suo pugno, e sbagliare la soglia costa in tutte e due le
/// direzioni: troppo stretta e il gioco non reagisce a una frase giusta con un
/// accento storto; troppo larga e Matteo va nel panico perche' hai nominato una
/// porta.
public class FraseTests
{
    [Test]
    public void QuellaGiustaSiRiconosce()
    {
        Assert.That(Frase.Detta("Chi passa per primo tiene la porta"), Is.True);
    }

    [Test]
    public void SiRiconosceDentroUnaFrasePiuLunga()
    {
        Assert.That(Frase.Detta("Senti… mi dice qualcosa «chi passa per primo tiene la porta»?"), Is.True);
    }

    [Test]
    public void MaiuscoleAccentiEPunteggiaturaNonContano()
    {
        Assert.That(Frase.Detta("CHI PASSA PER PRIMO, TIENE LA PORTA!"), Is.True);
    }

    /// Chi la ricopia a memoria si mangia una parola. Una sbagliata e' un errore
    /// di battitura; due sono un'altra frase.
    [Test]
    public void UnaParolaFuoriPostoNonBastaAPerderla()
    {
        Assert.That(Frase.Detta("chi entra per primo tiene la porta"), Is.True, "una sola diversa");
        Assert.That(Frase.Detta("chi entra per primo apre la porta"), Is.False, "due diverse: e' un'altra cosa");
    }

    [Test]
    public void NominareUnaPortaNonELaFormula()
    {
        Assert.That(Frase.Detta("Ho bussato alla porta e non ha aperto nessuno."), Is.False);
        Assert.That(Frase.Detta("Sei stato tu il primo ad arrivare?"), Is.False);
        Assert.That(Frase.Detta(""), Is.False);
    }

    /// La parola dev'essere quella, non una che la contiene: «riportare» non e'
    /// «porta», e «primogenito» non e' «primo».
    [Test]
    public void UnaParolaDentroUnAltraNonConta()
    {
        Assert.That(Frase.Detta("Devo riportare il primogenito a casa e tienimi il posto"), Is.False);
    }
}
