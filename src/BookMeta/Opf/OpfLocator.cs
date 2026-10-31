using BookMeta.Epub;
using MediaBrowser.Model.Logging;

namespace BookMeta.Opf;

public enum OpfSourceKind { Sidecar, EpubInternal }

/// <summary>OPF lu et sa provenance (<see cref="SourcePath"/> : fichier <c>metadata.opf</c> ou epub).</summary>
public sealed record OpfSource(OpfDocument Document, OpfSourceKind Kind, string SourcePath);

/// <summary>Trouve l'OPF d'un livre : <c>metadata.opf</c> du dossier, sinon OPF interne de l'epub. Ne lève jamais.</summary>
public static class OpfLocator
{
    public const string SidecarName = "metadata.opf";

    /// <param name="itemPath">Fichier du livre.</param>
    /// <param name="containingFolder">Dossier du livre ; déduit de <paramref name="itemPath"/> si nul.</param>
    /// <param name="listFilePaths">Liste les fichiers d'un dossier (ex. <c>IDirectoryService.GetFilePaths</c>) ; défaut : système de fichiers.</param>
    public static OpfSource? Locate(string? itemPath, string? containingFolder, Func<string, IEnumerable<string>>? listFilePaths, ILogger? logger)
    {
        try
        {
            var folder = !string.IsNullOrEmpty(containingFolder) ? containingFolder
                : string.IsNullOrEmpty(itemPath) ? null : Path.GetDirectoryName(itemPath);

            var sidecar = FindSidecar(folder, listFilePaths, logger);
            if (sidecar != null)
            {
                try
                {
                    using var fs = new FileStream(sidecar, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (OpfParser.Parse(fs, out var doc, out var err) && doc != null)
                        return new OpfSource(doc, OpfSourceKind.Sidecar, sidecar);
                    logger?.Warn("BookMeta : OPF illisible {0} ({1})", sidecar, err ?? "?");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger?.Warn("BookMeta : OPF inaccessible {0} ({1})", sidecar, ex.Message);
                }
            }

            if (!string.IsNullOrEmpty(itemPath) && itemPath.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            {
                if (!EpubArchive.TryOpen(itemPath, out var epub, out var openErr))
                {
                    logger?.Warn("BookMeta : epub illisible {0} ({1})", itemPath, openErr ?? "?");
                }
                else
                {
                    using (epub)
                    {
                        using var s = epub!.OpenOpf();
                        if (s == null) logger?.Warn("BookMeta : pas d'OPF dans l'epub {0}", itemPath);
                        else if (OpfParser.Parse(s, out var doc, out var err) && doc != null)
                            return new OpfSource(doc, OpfSourceKind.EpubInternal, itemPath);
                        else logger?.Warn("BookMeta : OPF interne illisible dans {0} ({1})", itemPath, err ?? "?");
                    }
                }
            }

            logger?.Debug("BookMeta : aucun OPF pour {0}", itemPath ?? "?");
            return null;
        }
        catch (Exception ex)
        {
            logger?.Warn("BookMeta : échec de lecture OPF pour {0} ({1})", itemPath ?? "?", ex.Message);
            return null;
        }
    }

    /// <summary>Chemin de <c>metadata.opf</c> dans <paramref name="folder"/> (nom insensible à la casse), ou <c>null</c>.</summary>
    public static string? FindSidecar(string? folder, Func<string, IEnumerable<string>>? listFilePaths, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(folder)) return null;
        try
        {
            var files = listFilePaths != null ? listFilePaths(folder) : (Directory.Exists(folder) ? Directory.EnumerateFiles(folder) : Enumerable.Empty<string>());
            return files.FirstOrDefault(f => string.Equals(Path.GetFileName(f), SidecarName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            logger?.Warn("BookMeta : dossier illisible {0} ({1})", folder, ex.Message);
            return null;
        }
    }
}
