using System.IO.Compression;
using MediaBrowser.Model.Logging;

namespace BookMeta.Covers;

/// <summary>Couverture d'un cbz : première image (tri naturel du nom), <c>__MACOSX</c> et fichiers cachés exclus. Ne lève jamais.</summary>
public static class CbzCoverExtractor
{
    public static CoverImage? Extract(string cbzPath, ILogger? logger)
    {
        try
        {
            using var fs = new FileStream(cbzPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
            var entry = zip.Entries
                .Where(e => e.Length > 0 && IsCandidate(e.FullName))
                .OrderBy(e => e.FullName.Replace('\\', '/'), NaturalComparer.Instance)
                .FirstOrDefault();
            var format = entry == null ? null : ImageFormats.FromExtension(Path.GetExtension(entry.Name));
            if (entry == null || format == null)
            {
                logger?.Debug("BookMeta : aucune image dans {0}", cbzPath);
                return null;
            }
            if (entry.Length > Epub.EpubArchive.MaxEntryBytes)
            {
                logger?.Warn("BookMeta : image trop volumineuse dans {0}", cbzPath);
                return null;
            }
            using var src = entry.Open();
            var ms = new MemoryStream();
            var buf = new byte[16384];
            int n;
            while ((n = src.Read(buf, 0, buf.Length)) > 0)
            {
                if (ms.Length + n > Epub.EpubArchive.MaxEntryBytes) { ms.Dispose(); return null; }
                ms.Write(buf, 0, n);
            }
            ms.Position = 0;
            return new CoverImage(ms, format.Value);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
        {
            logger?.Warn("BookMeta : cbz illisible {0} ({1})", cbzPath, ex.Message);
            return null;
        }
    }

    private static bool IsCandidate(string fullName)
    {
        var parts = fullName.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        if (parts.Any(p => p.Equals("__MACOSX", StringComparison.OrdinalIgnoreCase) || p.StartsWith('.'))) return false;
        return ImageFormats.FromExtension(Path.GetExtension(parts[^1])) != null;
    }

    /// <summary>Tri naturel insensible à la casse : "page2" &lt; "page10".</summary>
    internal sealed class NaturalComparer : IComparer<string>
    {
        public static readonly NaturalComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            x ??= string.Empty; y ??= string.Empty;
            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
                {
                    int si = i, sj = j;
                    while (i < x.Length && char.IsDigit(x[i])) i++;
                    while (j < y.Length && char.IsDigit(y[j])) j++;
                    var a = x.Substring(si, i - si).TrimStart('0');
                    var b = y.Substring(sj, j - sj).TrimStart('0');
                    if (a.Length != b.Length) return a.Length < b.Length ? -1 : 1;
                    var c = string.CompareOrdinal(a, b);
                    if (c != 0) return c;
                }
                else
                {
                    var c = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[j]));
                    if (c != 0) return c;
                    i++; j++;
                }
            }
            return (x.Length - i).CompareTo(y.Length - j);
        }
    }
}
