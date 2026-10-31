using BookMeta.Covers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace BookMeta.Providers;

/// <summary>Couverture embarquée : epub (si l'option est active) ou cbz.</summary>
public class BookEmbeddedCoverProvider : IDynamicImageProvider
{
    private readonly ILogger _logger;

    public BookEmbeddedCoverProvider(ILogManager logManager, IFileSystem fileSystem)
    {
        _logger = logManager.GetLogger("BookMeta");
    }

    /// <summary>Lecture de l'option <c>EnableEpubCoverExtraction</c> (défaut vrai si le plugin n'est pas chargé).</summary>
    internal Func<bool> EpubExtractionEnabled { get; set; } = () => Plugin.Instance?.Configuration.EnableEpubCoverExtraction ?? true;

    /// <summary>Clé des réglages de bibliothèque : ne jamais changer.</summary>
    public string Name => ProviderName;

    public const string ProviderName = "BookMeta Embedded Cover";

    public bool Supports(BaseItem item)
    {
        if (item is not Book) return false;
        // Emby construit la liste des « récupérateurs d'images » d'une bibliothèque avec un livre factice (chemin vide ou « …/dummy » sans
        // extension) : refuser ici ferait disparaître le provider des réglages. Le contrôle fin se fait avec un vrai chemin, puis dans GetImage.
        if (string.IsNullOrEmpty(item.Path)) return true;
        var ext = Path.GetExtension(item.Path);
        // Le livre factice de ProviderManager.GetPluginSummary a pour chemin « <métadonnées>/dummy » : aucune extension.
        // Un vrai livre en a toujours une.
        if (ext.Length == 0) return true;
        if (ext.Equals(".cbz", StringComparison.OrdinalIgnoreCase)) return true;
        return ext.Equals(".epub", StringComparison.OrdinalIgnoreCase) && EpubExtractionEnabled();
    }

    public ImageType[] GetSupportedImages(BaseItem item) => new[] { ImageType.Primary };

    public Task<DynamicImageResponse> GetImage(BaseMetadataResult itemResult, ImageType type, CancellationToken cancellationToken)
    {
        // Réponse vide (ni Stream ni Path) = « pas d'image » ; jamais d'exception vers Emby.
        var none = new DynamicImageResponse();
        try
        {
            // Premier item porteur d'un Path : BaseItem du résultat, sinon l'Item du MetadataResult<Book> (parfois sans Path).
            var item = new BaseItem?[] { itemResult.BaseItem, (itemResult as MetadataResult<Book>)?.Item }
                .FirstOrDefault(i => i != null && !string.IsNullOrEmpty(i.Path));
            if (item == null)
            {
                _logger.Debug("BookMeta : couverture embarquée ignorée, aucun item avec chemin");
                return Task.FromResult(none);
            }
            if (type != ImageType.Primary || !Supports(item)) return Task.FromResult(none);
            var cover = Path.GetExtension(item.Path).Equals(".cbz", StringComparison.OrdinalIgnoreCase)
                ? CbzCoverExtractor.Extract(item.Path, _logger)
                : EpubCoverExtractor.Extract(item.Path, _logger);
            if (cover == null) return Task.FromResult(none);
            return Task.FromResult(new DynamicImageResponse { Stream = cover.Data, Format = cover.Format });
        }
        catch (Exception ex)
        {
            _logger.Warn("BookMeta : couverture embarquée échouée ({0})", ex.Message);
            return Task.FromResult(none);
        }
    }
}
