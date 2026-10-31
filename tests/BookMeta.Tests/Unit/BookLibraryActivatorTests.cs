using BookMeta.Startup;
using BookMeta.Tests.Support;
using MediaBrowser.Model.Configuration;
using Xunit;

namespace BookMeta.Tests.Unit;

public class BookLibraryActivatorTests
{
    private const string Fetcher = "BookMeta Embedded Cover";

    private sealed class FakeStore : IBookLibraryStore
    {
        public List<BookLibrary> Libraries { get; } = new();
        public List<string> Saved { get; } = new();
        public Dictionary<string, LibraryOptions> SavedOptions { get; } = new();
        public TypeOptions? DefaultBook { get; set; } = new TypeOptions
        {
            Type = "Book", ImageFetchers = new[] { "Emby Defaut" }, ImageFetcherOrder = new[] { "Emby Defaut" }, MetadataFetchers = new[] { "MetaDefaut" },
        };
        public Func<BookLibrary, bool> FailOn { get; set; } = _ => false;
        public bool FailListing { get; set; }

        public IReadOnlyList<BookLibrary> GetBookLibraries() =>
            FailListing ? throw new InvalidOperationException("liste indisponible") : Libraries;

        public TypeOptions? GetDefaultBookTypeOptions() => DefaultBook;

        public LibraryOptions Clone(LibraryOptions o) => new()
        {
            ContentType = o.ContentType,
            TypeOptions = (o.TypeOptions ?? Array.Empty<TypeOptions>()).Select(t => new TypeOptions
            {
                Type = t.Type,
                ImageFetchers = t.ImageFetchers?.ToArray()!,
                ImageFetcherOrder = t.ImageFetcherOrder?.ToArray()!,
                MetadataFetchers = t.MetadataFetchers?.ToArray()!,
                MetadataFetcherOrder = t.MetadataFetcherOrder?.ToArray()!,
            }).ToArray(),
        };

        public void Save(BookLibrary library)
        {
            if (FailOn(library)) throw new IOException("disque plein");
            Saved.Add(library.Id);
            SavedOptions[library.Id] = library.Options;
        }
    }

    private static BookLibrary Lib(string id, string name, params TypeOptions[] types) =>
        new(id, name, new LibraryOptions { ContentType = "books", TypeOptions = types });

    private static (int Count, List<string> Ids, int Persisted, RecordingLogger Log) Run(FakeStore store, List<string>? ids = null)
    {
        var log = new RecordingLogger();
        ids ??= new List<string>();
        var persisted = 0;
        var n = BookLibraryActivator.Run(store, Fetcher, ids, () => persisted++, log.Logger);
        return (n, ids, persisted, log);
    }

    [Fact]
    public void SansEntreeBook_ElleReprendLesReglagesParDefautDEmby_PlusLeFetcher()
    {
        var store = new FakeStore();
        var lib = Lib("1", "Livres");
        store.Libraries.Add(lib);

        var r = Run(store);

        Assert.Equal(1, r.Count);
        var book = Assert.Single(store.SavedOptions["1"].TypeOptions);
        Assert.Equal("Book", book.Type);
        Assert.Equal(new[] { "Emby Defaut", Fetcher }, book.ImageFetchers);
        Assert.Equal(new[] { "Emby Defaut", Fetcher }, book.ImageFetcherOrder);
        Assert.Equal(new[] { "MetaDefaut" }, book.MetadataFetchers);
        Assert.Empty(lib.Options.TypeOptions); // l'instance du serveur n'a pas été modifiée en place
        Assert.Equal(new[] { "1" }, store.Saved);
        Assert.Equal(new[] { "1" }, r.Ids);
        Assert.Equal(1, r.Persisted);
        Assert.Contains(r.Log.Entries, e => e.Level == "Info" && e.Message.Contains("activé pour la bibliothèque Livres"));
    }

    [Fact]
    public void OptionsAvecTypeOptionsNul_SontGerees()
    {
        var store = new FakeStore();
        store.Libraries.Add(new BookLibrary("1", "Livres", new LibraryOptions { ContentType = "books", TypeOptions = null! }));
        Assert.Equal(1, Run(store).Count);
        Assert.Single(store.SavedOptions["1"].TypeOptions);
    }

    [Fact]
    public void ReglagesParDefautIndisponibles_AucuneEntreeVideN_EstCreee_InfoActivationManuelle()
    {
        var store = new FakeStore { DefaultBook = null };
        store.Libraries.Add(Lib("1", "Livres"));

        var r = Run(store);

        Assert.Equal(0, r.Count);
        Assert.Empty(store.Saved);
        Assert.Empty(r.Ids);
        Assert.Contains(r.Log.Entries, e => e.Level == "Info" && e.Message.Contains("à la main"));
    }

    [Fact]
    public void ListeVide_TraceDebug()
    {
        var r = Run(new FakeStore());
        Assert.Contains(r.Log.Entries, e => e.Level == "Debug" && e.Message.Contains("aucune bibliothèque de livres trouvée"));
    }

    [Fact]
    public void EntreeBookExistante_ConserveLesAutresFetchers_EtLOrdre()
    {
        var store = new FakeStore();
        var lib = Lib("1", "Livres",
            new TypeOptions { Type = "Movie", ImageFetchers = new[] { "TheMovieDb" } },
            new TypeOptions { Type = "book", ImageFetchers = new[] { "Autre" }, ImageFetcherOrder = new[] { "Autre" }, MetadataFetchers = new[] { "X" } });
        store.Libraries.Add(lib);

        Run(store);

        var saved = store.SavedOptions["1"];
        var book = saved.TypeOptions.Single(t => t.Type == "book");
        Assert.Equal(new[] { "Autre", Fetcher }, book.ImageFetchers);
        Assert.Equal(new[] { "Autre", Fetcher }, book.ImageFetcherOrder);
        Assert.Equal(new[] { "X" }, book.MetadataFetchers);
        Assert.Equal(new[] { "TheMovieDb" }, saved.TypeOptions.Single(t => t.Type == "Movie").ImageFetchers);
        Assert.Equal(new[] { "Autre" }, lib.Options.TypeOptions.Single(t => t.Type == "book").ImageFetchers); // original intact
    }

    [Fact]
    public void DejaActif_RienEstEnregistre_MaisLaBibliothequeEstMemorisee()
    {
        var store = new FakeStore();
        store.Libraries.Add(Lib("1", "Livres", new TypeOptions { Type = "Book", ImageFetchers = new[] { Fetcher }, ImageFetcherOrder = new[] { Fetcher } }));

        var r = Run(store);

        Assert.Equal(0, r.Count);
        Assert.Empty(store.Saved);
        Assert.Equal(new[] { "1" }, r.Ids);
    }

    [Fact]
    public void DejaTraitee_PuisDecochee_NEstPasRecochee()
    {
        var store = new FakeStore();
        var lib = Lib("1", "Livres", new TypeOptions { Type = "Book", ImageFetchers = Array.Empty<string>() });
        store.Libraries.Add(lib);

        var r = Run(store, new List<string> { "1" });

        Assert.Equal(0, r.Count);
        Assert.Empty(store.Saved);
        Assert.Empty(lib.Options.TypeOptions.Single().ImageFetchers);
        Assert.Equal(0, r.Persisted);
    }

    [Fact]
    public void BibliothequeCreeeApres_EstTraiteeSeule()
    {
        var store = new FakeStore();
        store.Libraries.Add(Lib("1", "Anciens"));
        store.Libraries.Add(Lib("2", "Nouveaux"));

        var r = Run(store, new List<string> { "1" });

        Assert.Equal(1, r.Count);
        Assert.Equal(new[] { "2" }, store.Saved);
        Assert.Equal(new[] { "1", "2" }, r.Ids);
    }

    [Fact]
    public void EchecDeSauvegarde_Warn_PasMemorisee_LesAutresContinuent()
    {
        var store = new FakeStore { FailOn = l => l.Id == "1" };
        store.Libraries.Add(Lib("1", "Cassée"));
        store.Libraries.Add(Lib("2", "Saine"));

        var r = Run(store);

        Assert.Equal(1, r.Count);
        Assert.Equal(new[] { "2" }, r.Ids);
        Assert.True(r.Log.HasWarning);
        Assert.Contains(r.Log.Warnings, w => w.Contains("Cassée") && w.Contains("disque plein"));
    }

    [Fact]
    public void ListeDesBibliothequesIndisponible_Warn_SansException()
    {
        var r = Run(new FakeStore { FailListing = true });
        Assert.Equal(0, r.Count);
        Assert.True(r.Log.HasWarning);
    }

    [Fact]
    public void AucuneBibliothequeDeLivres_RienNeSePasse()
    {
        var r = Run(new FakeStore());
        Assert.Equal(0, r.Count);
        Assert.False(r.Log.HasWarning);
    }

    [Fact]
    public void ConfigurationParDefaut_ListeVide() =>
        Assert.Empty(new PluginConfiguration().AutoEnabledLibraryIds);
}
