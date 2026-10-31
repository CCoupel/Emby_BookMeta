using System.Net;
using System.Text.RegularExpressions;

namespace BookMeta.Opf;

/// <summary>Conversion d'une description (HTML éventuellement échappé) en texte brut.</summary>
public static class HtmlText
{
    private static readonly Regex EscapedMarkup = new(@"&lt;/?[a-zA-Z!]", RegexOptions.Compiled);
    private static readonly Regex BlockBreak = new(@"<\s*(br|/p|/div|/li|/h[1-6]|/tr)\b[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BlockStart = new(@"<\s*(p|div|li|h[1-6]|tr)\b[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Tag = new(@"<[a-zA-Z/!][^>]*>", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"[ \t\f\v]+", RegexOptions.Compiled);
    private static readonly Regex ManyBlankLines = new(@"\n{3,}", RegexOptions.Compiled);

    /// <summary>Retourne le texte brut, ou <c>null</c> si le résultat est vide.</summary>
    public static string? ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        var s = html;
        // Balisage double-échappé (&lt;p&gt;) : on le rend d'abord lisible comme balisage.
        if (EscapedMarkup.IsMatch(s)) s = WebUtility.HtmlDecode(s);

        s = BlockBreak.Replace(s, "\n");
        s = BlockStart.Replace(s, "\n");
        s = Tag.Replace(s, string.Empty);
        s = WebUtility.HtmlDecode(s);

        s = s.Replace("\r\n", "\n").Replace('\r', '\n').Replace(' ', ' ');
        s = Spaces.Replace(s, " ");
        s = string.Join("\n", s.Split('\n').Select(l => l.Trim()));
        s = ManyBlankLines.Replace(s, "\n\n").Trim();
        return s.Length == 0 ? null : s;
    }
}
