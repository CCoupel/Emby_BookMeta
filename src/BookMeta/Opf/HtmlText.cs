using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BookMeta.Opf;

/// <summary>Conversion d'une description (HTML éventuellement échappé) en texte brut. Linéaire et borné : jamais d'exception.</summary>
public static class HtmlText
{
    /// <summary>Longueur maximale traitée (au-delà, le texte est tronqué).</summary>
    public const int MaxLength = 64 * 1024;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);
    private const RegexOptions Opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    // Classes de caractères [^<>] : aucune ambiguïté entre deux balises, donc pas de retour arrière quadratique.
    private static readonly Regex EscapedMarkup = new(@"&lt;/?[a-zA-Z!]", Opts, Timeout);
    private static readonly Regex BlockBreak = new(@"<[ \t\r\n]*(?:br|/p|/div|/li|/h[1-6]|/tr)\b[^<>]*>", Opts | RegexOptions.IgnoreCase, Timeout);
    private static readonly Regex BlockStart = new(@"<[ \t\r\n]*(?:p|div|li|h[1-6]|tr)\b[^<>]*>", Opts | RegexOptions.IgnoreCase, Timeout);
    private static readonly Regex Tag = new(@"<[a-zA-Z/!][^<>]*>", Opts, Timeout);
    private static readonly Regex Spaces = new(@"[ \t\f\v]+", Opts, Timeout);
    private static readonly Regex ManyBlankLines = new(@"\n{3,}", Opts, Timeout);

    /// <summary>Retourne le texte brut, ou <c>null</c> si le résultat est vide.</summary>
    public static string? ToPlainText(string? html) => ToPlainText(html, out _);

    /// <param name="degraded"><c>true</c> si un délai de traitement a été dépassé (repli sur un nettoyage linéaire simple).</param>
    public static string? ToPlainText(string? html, out bool degraded)
    {
        degraded = false;
        if (string.IsNullOrWhiteSpace(html)) return null;
        var s = html.Length > MaxLength ? html.Substring(0, MaxLength) : html;
        try
        {
            // Balisage double-échappé (&lt;p&gt;) : on le rend d'abord lisible comme balisage.
            if (EscapedMarkup.IsMatch(s)) s = WebUtility.HtmlDecode(s);

            s = BlockBreak.Replace(s, "\n");
            s = BlockStart.Replace(s, "\n");
            s = Tag.Replace(s, string.Empty);
            s = WebUtility.HtmlDecode(s);

            s = s.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\u00A0', ' ');
            s = Spaces.Replace(s, " ");
            s = string.Join("\n", s.Split('\n').Select(l => l.Trim()));
            s = ManyBlankLines.Replace(s, "\n\n").Trim();
        }
        catch (RegexMatchTimeoutException)
        {
            degraded = true;
            s = StripLinear(s);
        }
        return s.Length == 0 ? null : s;
    }

    /// <summary>Repli : retire tout ce qui est entre « &lt; » et « &gt; » en un seul passage, sans expression régulière.</summary>
    private static string StripLinear(string s)
    {
        var sb = new StringBuilder(s.Length);
        var inTag = false;
        foreach (var c in s)
        {
            if (c == '<') inTag = true;
            else if (c == '>' && inTag) { inTag = false; sb.Append(' '); }
            else if (!inTag) sb.Append(c);
        }
        return WebUtility.HtmlDecode(sb.ToString()).Trim();
    }
}
