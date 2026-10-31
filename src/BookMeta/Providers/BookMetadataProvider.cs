using BookMeta.Opf;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace BookMeta.Providers;

/// <summary>Lecteur local de métadonnées des livres (metadata.opf, sinon OPF interne de l'epub). Aucun réseau, aucune écriture.</summary>
public class BookMetadataProvider : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
{
    private readonly ILogger _logger;

    public BookMetadataProvider(ILogManager logManager, IFileSystem fileSystem) // fileSystem : injecté par Emby, non utilisé (System.IO)
    {
        _logger = logManager.GetLogger("BookMeta");
    }

    /// <summary>
    /// Sans effet depuis le cycle 4 (le tri est verrouillé par <see cref="BookSortNameProvider"/>, sur l'instance rafraîchie).
    /// Conservé uniquement pour que <c>tests/Support/SpecApi.cs</c> (test-writer) continue de compiler ; à supprimer avec lui.
    /// </summary>
    internal Func<long, BaseItem?>? ItemLookup { get; set; }

    /// <summary>Clé affichée (et stockée) dans les réglages de bibliothèque : ne jamais changer.</summary>
    public string Name => "BookMeta";

    public Task<MetadataResult<Book>> GetMetadata(ItemInfo info, LibraryOptions libraryOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Book>();
        try
        {
            // Dossier dérivé de info.Path : ContainingFolderPath dépend d'un état statique d'Emby (BaseItem.FileSystem).
            Func<string, IEnumerable<string>> list = folder => directoryService.GetFilePaths(folder);
            var source = OpfLocator.Locate(info.Path, null, list, _logger, cancellationToken);
            var book = source == null ? null : BookMapper.Map(source.Document);
            if (book != null)
            {
                result.Item = book;
                result.HasMetadata = true;
                result.ResultLanguage = source!.Document.Language;
                result.Provider = BookMapper.ProviderName;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Warn("BookMeta : échec de lecture des métadonnées de {0} ({1})", info?.Path ?? "?", ex.Message);
            result = new MetadataResult<Book>();
        }
        return Task.FromResult(result);
    }

    /// <summary>Vrai si <c>metadata.opf</c> (sinon le fichier du livre) est plus récent que la dernière actualisation.</summary>
    public bool HasChanged(BaseItem item, LibraryOptions libraryOptions, IDirectoryService directoryService)
    {
        try
        {
            var path = item.Path;
            var folder = string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path);
            var sidecar = OpfLocator.FindSidecar(folder, f => directoryService.GetFilePaths(f), _logger);
            var watched = sidecar ?? path;
            if (string.IsNullOrEmpty(watched) || !File.Exists(watched)) return false;
            // Fichiers locaux : System.IO suffit (la bibliothèque est en lecture seule, aucun autre FS).
            return new DateTimeOffset(File.GetLastWriteTimeUtc(watched), TimeSpan.Zero) > item.DateLastRefreshed;
        }
        catch (Exception ex)
        {
            _logger.Warn("BookMeta : HasChanged a échoué pour {0} ({1})", item?.Path ?? "?", ex.Message);
            return false;
        }
    }
}
