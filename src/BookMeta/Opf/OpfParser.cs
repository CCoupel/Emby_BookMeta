using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace BookMeta.Opf;

/// <summary>
/// Lit un OPF 2.0 (Calibre/Bindery) ou 3.x. Tolérant : espaces de noms et préfixes d'attributs (<c>opf:</c>) facultatifs.
/// Sécurisé : DTD ignorée, aucun résolveur externe, taille d'entrée bornée. Ne lève jamais d'exception.
/// </summary>
public static class OpfParser
{
    public const int MaxBytes = 5 * 1024 * 1024;

    /// <summary>Analyse un flux OPF. Retourne <c>false</c> (et <paramref name="doc"/> nul) si illisible.</summary>
    public static bool Parse(Stream stream, out OpfDocument? doc) => Parse(stream, out doc, out _);

    public static bool Parse(Stream stream, out OpfDocument? doc, out string? error)
    {
        doc = null;
        error = null;
        try
        {
            var buffer = ReadBounded(stream, MaxBytes);
            if (buffer == null)
            {
                error = $"OPF trop volumineux (> {MaxBytes} octets)";
                return false;
            }
            using var ms = new MemoryStream(buffer);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                CheckCharacters = false,
                MaxCharactersFromEntities = 1024,
            };
            using var reader = XmlReader.Create(ms, settings);
            var xml = XDocument.Load(reader, LoadOptions.None);
            doc = Build(xml);
            if (doc == null) error = "racine <package> / <metadata> introuvable";
            return doc != null;
        }
        catch (Exception ex) when (ex is XmlException or IOException or InvalidDataException or NotSupportedException
                                       or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            error = ex.Message;
            doc = null;
            return false;
        }
    }

    public static bool Parse(string xml, out OpfDocument? doc)
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml ?? string.Empty));
        return Parse(ms, out doc, out _);
    }

    internal static byte[]? ReadBounded(Stream stream, int max)
    {
        using var ms = new MemoryStream();
        var buf = new byte[8192];
        int n;
        while ((n = stream.Read(buf, 0, buf.Length)) > 0)
        {
            if (ms.Length + n > max) return null;
            ms.Write(buf, 0, n);
        }
        return ms.ToArray();
    }

    // ---------------------------------------------------------------- construction

    private static OpfDocument? Build(XDocument xml)
    {
        var root = xml.Root;
        if (root == null || !Is(root, "package")) return null;
        var metadata = root.Elements().FirstOrDefault(e => Is(e, "metadata"));
        if (metadata == null) return null;

        var metas = metadata.Elements().Where(e => Is(e, "meta")).ToList();
        var dc = metadata.Elements().Where(e => !Is(e, "meta") && !Is(e, "link")).ToList();

        // Raffinements OPF 3 : id -> (property -> valeur)
        var refines = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var m in metas)
        {
            var r = Attr(m, "refines");
            var p = Attr(m, "property");
            if (r == null || p == null || !r.StartsWith('#')) continue;
            var value = Clean(m.Value);
            if (value == null) continue;
            var id = r.Substring(1);
            if (!refines.TryGetValue(id, out var props)) refines[id] = props = new(StringComparer.OrdinalIgnoreCase);
            props.TryAdd(p, value);
        }
        string? Refined(XElement e, string property)
        {
            var id = Attr(e, "id");
            return id != null && refines.TryGetValue(id, out var props) && props.TryGetValue(property, out var v) ? v : null;
        }
        string? MetaContent(string name) => metas
            .Where(m => string.Equals(Attr(m, "name"), name, StringComparison.OrdinalIgnoreCase))
            .Select(m => Clean(Attr(m, "content"))).FirstOrDefault(v => v != null);

        var titleEl = dc.FirstOrDefault(e => Is(e, "title") && Clean(e.Value) != null);
        var title = titleEl == null ? null : Clean(titleEl.Value);
        var titleSort = MetaContent("calibre:title_sort") ?? (titleEl == null ? null : Refined(titleEl, "file-as"));

        var creators = new List<OpfCreator>();
        foreach (var c in dc.Where(e => Is(e, "creator")))
        {
            var name = Clean(c.Value);
            if (name == null) continue;
            creators.Add(new OpfCreator(name, Clean(Attr(c, "file-as")) ?? Refined(c, "file-as"),
                Clean(Attr(c, "role")) ?? Refined(c, "role")));
        }

        var identifiers = new List<OpfIdentifier>();
        foreach (var i in dc.Where(e => Is(e, "identifier")))
        {
            var value = Clean(i.Value);
            if (value == null) continue;
            var scheme = Clean(Attr(i, "scheme")) ?? Refined(i, "identifier-type");
            if (scheme == null && value.StartsWith("urn:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Split(':', 3);
                if (parts.Length == 3 && parts[2].Length > 0) { scheme = parts[1]; value = parts[2]; }
            }
            else if (scheme != null && value.StartsWith("urn:" + scheme + ":", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(scheme.Length + 5);
            }
            identifiers.Add(new OpfIdentifier((scheme ?? string.Empty).ToUpperInvariant(), value));
        }

        // Série : Calibre, sinon OPF 3 belongs-to-collection
        var series = MetaContent("calibre:series");
        double? seriesIndex = ParseDouble(MetaContent("calibre:series_index"));
        if (series == null)
        {
            foreach (var m in metas.Where(m => string.Equals(Attr(m, "property"), "belongs-to-collection", StringComparison.OrdinalIgnoreCase)))
            {
                var type = Refined(m, "collection-type");
                var name = Clean(m.Value);
                if (name == null || (type != null && !string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))) continue;
                series = name;
                seriesIndex ??= ParseDouble(Refined(m, "group-position"));
                break;
            }
        }

        var manifest = new List<OpfManifestItem>();
        var manifestEl = root.Elements().FirstOrDefault(e => Is(e, "manifest"));
        if (manifestEl != null)
            foreach (var item in manifestEl.Elements().Where(e => Is(e, "item")))
            {
                var href = Clean(Attr(item, "href"));
                if (href != null) manifest.Add(new OpfManifestItem(Attr(item, "id"), href, Clean(Attr(item, "media-type")), Clean(Attr(item, "properties"))));
            }

        var descEl = dc.FirstOrDefault(e => Is(e, "description"));
        var degraded = false;
        var description = descEl == null ? null : HtmlText.ToPlainText(descEl.Value, out degraded);
        return new OpfDocument
        {
            Title = title,
            TitleSort = titleSort,
            Creators = creators,
            Description = description,
            DescriptionDegraded = degraded,
            Date = ParseDate(dc.Where(e => Is(e, "date") && IsPublicationDate(e)).Select(e => Clean(e.Value)).FirstOrDefault(v => v != null)),
            Language = dc.Where(e => Is(e, "language")).Select(e => Clean(e.Value)).FirstOrDefault(v => v != null),
            Publishers = Distinct(dc.Where(e => Is(e, "publisher")).Select(e => Clean(e.Value))),
            Subjects = Distinct(dc.Where(e => Is(e, "subject")).Select(e => Clean(e.Value))),
            Series = series,
            SeriesIndex = seriesIndex,
            Identifiers = identifiers,
            Manifest = manifest,
            CoverId = MetaContent("cover"),
        };
    }

    // ---------------------------------------------------------------- utilitaires

    /// <summary><c>opf:event</c> absent ou « publication » (OPF 2) ; « creation » / « modification » sont ignorées.</summary>
    private static bool IsPublicationDate(XElement e)
    {
        var ev = Clean(Attr(e, "event"));
        return ev == null || string.Equals(ev, "publication", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Is(XElement e, string localName) => e.Name.LocalName == localName;

    /// <summary>Attribut sans espace de noms, sinon portant n'importe quel espace de noms (ex. <c>opf:role</c>).</summary>
    private static string? Attr(XElement e, string localName)
    {
        var a = e.Attribute(localName) ?? e.Attributes().FirstOrDefault(x => x.Name.LocalName == localName && !x.IsNamespaceDeclaration);
        return a?.Value;
    }

    private static string? Clean(string? s)
    {
        if (s == null) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }

    private static IReadOnlyList<string> Distinct(IEnumerable<string?> values) =>
        values.Where(v => v != null).Select(v => v!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static double? ParseDouble(string? s) =>
        s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && !double.IsNaN(d) && !double.IsInfinity(d) ? d : null;

    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "yyyy-MM", "yyyy",
        "yyyy-MM-ddTHH:mm:ssK", "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", "yyyy-MM-ddTHH:mmK", "yyyy-MM-ddTHH:mm:ss",
    };

    /// <summary>
    /// Formats <c>yyyy-MM-dd</c>, <c>yyyy-MM</c>, <c>yyyy</c>, ISO 8601 complet ; formats ambigus (<c>1/2/2020</c>) refusés.
    /// Le décalage d'origine est conservé (l'année reste celle écrite dans l'OPF). Invalide ou année &lt; 1000 (valeur « inconnue » de Calibre) → <c>null</c>.
    /// </summary>
    public static DateTimeOffset? ParseDate(string? s)
    {
        s = Clean(s);
        if (s == null) return null;
        if (!DateTimeOffset.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d))
            return null;
        return d.Year < 1000 ? null : d;
    }
}
