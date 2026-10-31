using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace BookMeta.Epub;

/// <summary>
/// Lecture seule d'un epub (zip) : <c>META-INF/container.xml</c> → rootfile OPF, entrées par chemin.
/// Les entrées sont copiées en mémoire (bornées) pour rester utilisables après <see cref="Dispose"/>.
/// </summary>
public sealed class EpubArchive : IDisposable
{
    public const int MaxEntryBytes = 32 * 1024 * 1024;

    private readonly ZipArchive _zip;
    private readonly Dictionary<string, ZipArchiveEntry> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ZipArchiveEntry> _entriesIgnoreCase = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ouvre l'archive ; propriété du flux transmise à l'instance. Lève <see cref="InvalidDataException"/> si illisible.</summary>
    public EpubArchive(Stream stream)
    {
        _zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        foreach (var e in _zip.Entries)
        {
            if (e.FullName.EndsWith('/')) continue;
            var name = NormalizeEntryName(e.FullName);
            _entries.TryAdd(name, e);
            _entriesIgnoreCase.TryAdd(name, e);
        }
    }

    /// <summary>Ouvre un fichier epub ; <c>false</c> (avec <paramref name="error"/>) si absent, corrompu ou inaccessible. Ne lève jamais.</summary>
    public static bool TryOpen(string path, out EpubArchive? archive, out string? error)
    {
        archive = null;
        error = null;
        FileStream? fs = null;
        try
        {
            fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            archive = new EpubArchive(fs);
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            fs?.Dispose();
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Chemin (dans l'archive) du premier rootfile de <c>META-INF/container.xml</c>, ou <c>null</c>.</summary>
    public string? RootfilePath
    {
        get
        {
            try
            {
                using var s = OpenEntry("META-INF/container.xml");
                if (s == null) return null;
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, CheckCharacters = false };
                using var reader = XmlReader.Create(s, settings);
                var path = XDocument.Load(reader).Descendants()
                    .Where(e => e.Name.LocalName == "rootfile")
                    .Select(e => (string?)e.Attribute("full-path"))
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
                return path == null ? null : NormalizeEntryName(Uri.UnescapeDataString(path.Trim()));
            }
            catch (Exception ex) when (ex is XmlException or IOException or InvalidDataException or UriFormatException)
            {
                return null;
            }
        }
    }

    /// <summary>Flux (en mémoire) de l'OPF désigné par le rootfile, ou <c>null</c>.</summary>
    public Stream? OpenOpf()
    {
        var root = RootfilePath;
        return root == null ? null : OpenEntry(root);
    }

    /// <summary>
    /// Résout <paramref name="href"/> (relatif à l'OPF situé en <paramref name="opfPath"/>) en chemin d'entrée normalisé :
    /// décodage %xx, retrait de <c>#fragment</c>/<c>?query</c>, <c>..</c> et <c>.</c> résolus. <c>null</c> si l'href sort de l'archive ou est absolu (URL).
    /// </summary>
    public static string? ResolveHref(string opfPath, string href)
    {
        if (string.IsNullOrWhiteSpace(href) || href.Contains("://") || href.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
        var h = href.Trim();
        var cut = h.IndexOfAny(new[] { '#', '?' });
        if (cut >= 0) h = h.Substring(0, cut);
        if (h.Length == 0) return null;
        try { h = Uri.UnescapeDataString(h); } catch (UriFormatException) { return null; }
        h = h.Replace('\\', '/');

        var baseDir = NormalizeEntryName(opfPath ?? string.Empty);
        var slash = baseDir.LastIndexOf('/');
        baseDir = slash >= 0 ? baseDir.Substring(0, slash) : string.Empty;

        var segments = new List<string>();
        var full = h.StartsWith('/') ? h : (baseDir.Length > 0 ? baseDir + "/" + h : h);
        foreach (var seg in full.Split('/'))
        {
            if (seg.Length == 0 || seg == ".") continue;
            if (seg == "..")
            {
                if (segments.Count == 0) return null;
                segments.RemoveAt(segments.Count - 1);
            }
            else segments.Add(seg);
        }
        return segments.Count == 0 ? null : string.Join("/", segments);
    }

    public bool HasEntry(string path) => Find(path) != null;

    /// <summary>Contenu de l'entrée (copié en mémoire, borné à <see cref="MaxEntryBytes"/>), ou <c>null</c> si absente / trop grande / illisible.</summary>
    public Stream? OpenEntry(string path)
    {
        var entry = Find(path);
        if (entry == null || entry.Length > MaxEntryBytes) return null;
        try
        {
            using var src = entry.Open();
            var ms = new MemoryStream();
            var buf = new byte[16384];
            int n;
            while ((n = src.Read(buf, 0, buf.Length)) > 0)
            {
                if (ms.Length + n > MaxEntryBytes) { ms.Dispose(); return null; }
                ms.Write(buf, 0, n);
            }
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException or ObjectDisposedException)
        {
            return null;
        }
    }

    private ZipArchiveEntry? Find(string path)
    {
        var name = NormalizeEntryName(path);
        return _entries.TryGetValue(name, out var e) ? e : _entriesIgnoreCase.GetValueOrDefault(name);
    }

    private static string NormalizeEntryName(string name) => name.Replace('\\', '/').TrimStart('/');

    public void Dispose() => _zip.Dispose();
}
