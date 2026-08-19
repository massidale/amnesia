using Amnesia.Core;

namespace Amnesia.Llm;

/// La chiave del provider vive in un `.env` fuori dal controllo di versione.
/// Da qui esce solo dentro un Result e non entra mai in un messaggio d'errore:
/// un errore viaggia nei log e negli screenshot, una chiave no.
public static class EnvFile
{
    public const string OpenRouterKeyName = "OPENROUTER_KEY";

    /// Legge una chiave dal testo di un file .env. Il testo arriva dall'esterno
    /// cosi' che i test possano usare fixture proprie senza sfiorare il file vero.
    public static Result<string> ParseKey(string text, string name)
    {
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) || !trimmed.StartsWith(name, StringComparison.Ordinal))
            {
                continue;
            }

            var remainder = trimmed.Substring(name.Length).Trim();
            // Senza questo, OPENROUTER_KEY_EXTRA passerebbe per OPENROUTER_KEY.
            if (!remainder.StartsWith("=", StringComparison.Ordinal))
            {
                continue;
            }

            var value = Unquote(remainder.Substring(1).Trim());
            if (value.Length > 0)
            {
                return Result<string>.Ok(value);
            }
        }

        // Il messaggio dice cosa aggiungere e dove, e non ripete niente di cio'
        // che ha letto: il file puo' contenere altri segreti oltre a questo.
        return Result<string>.Fail("missing_api_key", $"aggiungi {name}=<chiave> al .env ignorato da git nella radice del progetto");
    }

    public static Result<string> LoadKey(string path, string name)
    {
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            return Result<string>.Fail("missing_api_key", $".env non leggibile: {path}");
        }

        return ParseKey(text, name);
    }

    public static Result<string> LoadOpenRouterKey(string path) => LoadKey(path, OpenRouterKeyName);

    /// Toglie una virgoletta davanti e una dietro, ciascuna solo se c'e': un
    /// valore mal quotato resta com'e' invece di perdere un carattere buono.
    private static string Unquote(string value)
    {
        if (value.Length > 0 && value[0] == '"')
        {
            value = value.Substring(1);
        }
        if (value.Length > 0 && value[value.Length - 1] == '"')
        {
            value = value.Substring(0, value.Length - 1);
        }
        return value;
    }
}
