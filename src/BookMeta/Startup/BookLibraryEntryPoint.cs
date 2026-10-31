using BookMeta.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace BookMeta.Startup;

/// <summary>
/// Point d'entrée serveur : au démarrage, active « BookMeta Embedded Cover » sur les bibliothèques de livres (décision utilisateur D2-bis ;
/// Emby ne coche par défaut que ses propres fournisseurs, voir docs/SDK_NOTES.md §11).
/// </summary>
public class BookLibraryEntryPoint : IServerEntryPoint
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IJsonSerializer _json;
    private readonly ILogger _logger;

    public BookLibraryEntryPoint(ILibraryManager libraryManager, IProviderManager providerManager, IJsonSerializer json, ILogManager logManager)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _json = json;
        _logger = logManager.GetLogger("BookMeta");
    }

    public void Run()
    {
        try
        {
            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                _logger.Warn("BookMeta : plugin non initialisé, activation du récupérateur de couvertures ignorée");
                return;
            }
            var config = plugin.Configuration;
            config.AutoEnabledLibraryIds ??= new List<string>();
            BookLibraryActivator.Run(new ServerStore(_libraryManager, _providerManager, _json), BookEmbeddedCoverProvider.ProviderName,
                config.AutoEnabledLibraryIds, () => plugin.SaveConfiguration(), _logger);
        }
        catch (Exception ex)
        {
            _logger.Warn("BookMeta : point d'entrée serveur en échec ({0})", ex.Message);
        }
    }

    public void Dispose()
    {
    }

    private sealed class ServerStore : IBookLibraryStore
    {
        private readonly ILibraryManager _manager;
        private readonly IProviderManager _providers;
        private readonly IJsonSerializer _json;

        public ServerStore(ILibraryManager manager, IProviderManager providers, IJsonSerializer json)
        {
            _manager = manager;
            _providers = providers;
            _json = json;
        }

        public TypeOptions? GetDefaultBookTypeOptions() =>
            _providers.GetDefaultLibraryOptions("books")?.TypeOptions?
                .FirstOrDefault(t => string.Equals(t.Type, BookLibraryActivator.BookType, StringComparison.OrdinalIgnoreCase));

        // Même aller-retour JSON que CollectionFolder.SaveLibraryOptions : LibraryOptions n'a pas de Clone.
        public LibraryOptions Clone(LibraryOptions options) =>
            _json.DeserializeFromString<LibraryOptions>(_json.SerializeToString(options));

        public IReadOnlyList<BookLibrary> GetBookLibraries() =>
            _manager.GetVirtualFolders()
                .Where(f => f.LibraryOptions != null && !string.IsNullOrEmpty(f.ItemId)
                            && (string.Equals(f.LibraryOptions.ContentType, "books", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(f.CollectionType, "books", StringComparison.OrdinalIgnoreCase)))
                .Select(f => new BookLibrary(f.ItemId, f.Name ?? f.ItemId, f.LibraryOptions))
                .ToList();

        public void Save(BookLibrary library)
        {
            var folder = long.TryParse(library.Id, out var id) ? _manager.GetItemById(id)
                : Guid.TryParse(library.Id, out var guid) ? _manager.GetItemById(guid) : null;
            if (folder is CollectionFolder collection) collection.UpdateLibraryOptions(library.Options);
            else if (long.TryParse(library.Id, out var internalId)) CollectionFolder.SaveLibraryOptions(internalId, library.Options);
            else throw new InvalidOperationException("bibliothèque introuvable : " + library.Id);
        }
    }
}
