using BookMeta.Covers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace BookMeta.Providers;

/// <summary>Couverture <c>cover.*</c> / <c>folder.*</c> du dossier du livre.</summary>
public class BookCoverFileProvider : ILocalImageFileProvider
{
    private readonly ILogger _logger;

    public BookCoverFileProvider(ILogManager logManager) => _logger = logManager.GetLogger("BookMeta");

    /// <summary>Clé des réglages de bibliothèque : ne jamais changer.</summary>
    public string Name => "BookMeta Cover File";

    public bool Supports(BaseItem item) => item is Book;

    public List<LocalImageInfo> GetImages(BaseItem item, LibraryOptions libraryOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        var result = new List<LocalImageInfo>();
        try
        {
            var folder = string.IsNullOrEmpty(item.Path) ? null : Path.GetDirectoryName(item.Path);
            if (string.IsNullOrEmpty(folder)) return result;
            var file = CoverFileLocator.Find(directoryService.GetFiles(folder));
            if (file != null) result.Add(new LocalImageInfo { FileInfo = file, Type = ImageType.Primary });
        }
        catch (Exception ex)
        {
            _logger.Warn("BookMeta : recherche de couverture échouée pour {0} ({1})", item?.Path ?? "?", ex.Message);
        }
        return result;
    }
}
