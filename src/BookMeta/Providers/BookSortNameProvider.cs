using System.Runtime.CompilerServices;
using BookMeta.Opf;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace BookMeta.Providers;

/// <summary>
/// Fournisseur « custom » exécuté par Emby en fin de chaîne de rafraîchissement (après la fusion des lecteurs locaux, AVANT
/// <c>BaseItem.AfterMetadataRefresh</c> qui réinitialise le tri). Il reçoit l'instance réellement rafraîchie et sauvegardée :
/// on y verrouille le champ <c>SortName</c> et on y pose le tri « Auteur - [Série NN -] Titre » (voir docs/SDK_NOTES.md §9).
/// </summary>
public class BookSortNameProvider : ICustomMetadataProvider<Book>, IForcedProvider
{
    private readonly ILogger _logger;

    public BookSortNameProvider(ILogManager logManager) => _logger = logManager.GetLogger("BookMeta");

    /// <summary>Non visible dans les réglages (fournisseur « custom ») ; nom stable par précaution.</summary>
    public string Name => "BookMeta Sort Name";

    public Task<ItemUpdateType> FetchAsync(MetadataResult<Book> itemResult, MetadataRefreshOptions options, LibraryOptions libraryOptions, CancellationToken cancellationToken)
    {
        var item = itemResult?.Item;
        try
        {
            if (item == null || string.IsNullOrEmpty(item.Path)) return Task.FromResult(ItemUpdateType.None);
            cancellationToken.ThrowIfCancellationRequested();

            var source = OpfLocator.Locate(item.Path, null, null, _logger, cancellationToken);
            if (source == null)
            {
                _logger.Debug("BookMeta : tri inchangé pour {0} (pas d'OPF)", item.Path);
                return Task.FromResult(ItemUpdateType.None);
            }

            var sortName = BookMapper.BuildSortName(source.Document);
            var wasLocked = item.LockedFields?.Contains(MetadataFields.SortName) == true;
            // Verrou déjà posé : SortName est la valeur stockée (pas de recalcul) ; sinon le getter calculerait un tri depuis le nom.
            var before = wasLocked ? item.SortName : null;
            SortNameLock.Apply(item, sortName);
            var changed = !wasLocked || !string.Equals(before, item.SortName, StringComparison.Ordinal);

            _logger.Debug("BookMeta : tri verrouillé pour {0} : {1} (item #{2}, instance {3}, verrou={4}, modifié={5})",
                item.Path, item.SortName, item.InternalId, RuntimeHelpers.GetHashCode(item),
                item.LockedFields?.Contains(MetadataFields.SortName) == true, changed);
            return Task.FromResult(changed ? ItemUpdateType.MetadataEdit : ItemUpdateType.None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Warn("BookMeta : tri non verrouillé pour {0} ({1})", item?.Path ?? "?", ex.Message);
            return Task.FromResult(ItemUpdateType.None);
        }
    }
}
