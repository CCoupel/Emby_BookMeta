using BookMeta.Epub;
using BookMeta.Opf;
using MediaBrowser.Model.Logging;

namespace BookMeta.Covers;

/// <summary>Couverture d'un epub : OPF 2 <c>meta cover</c> → OPF 3 <c>cover-image</c> → première image du manifest. Ne lève jamais.</summary>
public static class EpubCoverExtractor
{
    public static CoverImage? Extract(string epubPath, ILogger? logger)
    {
        try
        {
            if (!EpubArchive.TryOpen(epubPath, out var epub, out var err))
            {
                logger?.Warn("BookMeta : epub illisible {0} ({1})", epubPath, err ?? "?");
                return null;
            }
            using (epub)
            {
                var rootfile = epub!.RootfilePath;
                if (rootfile == null) { logger?.Warn("BookMeta : pas de rootfile dans {0}", epubPath); return null; }
                using var opfStream = epub.OpenEntry(rootfile, OpfParser.MaxBytes);
                OpfDocument? opf = null;
                string? perr = null;
                if (opfStream == null || !OpfParser.Parse(opfStream, out opf, out perr) || opf == null)
                {
                    logger?.Warn("BookMeta : OPF interne illisible dans {0} ({1})", epubPath, perr ?? "?");
                    return null;
                }
                var declared = 0;
                foreach (var item in opf.CoverCandidates())
                {
                    declared++;
                    var format = ImageFormats.FromMediaType(item.MediaType) ?? ImageFormats.FromExtension(Path.GetExtension(item.Href));
                    var entry = EpubArchive.ResolveHref(rootfile, item.Href);
                    if (format == null || entry == null) continue;
                    var data = epub.OpenEntry(entry);
                    if (data != null && data.Length > 0) return new CoverImage(data, format.Value);
                    data?.Dispose();
                }
                if (declared > 0) logger?.Warn("BookMeta : couverture déclarée mais illisible ou vide dans {0}", epubPath);
                else logger?.Debug("BookMeta : aucune image dans le manifest de {0}", epubPath);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger?.Warn("BookMeta : extraction de couverture epub échouée {0} ({1})", epubPath, ex.Message);
            return null;
        }
    }
}
