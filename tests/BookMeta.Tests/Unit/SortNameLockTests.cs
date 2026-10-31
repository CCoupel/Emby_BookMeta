using BookMeta.Providers;
using BookMeta.Tests.Support;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Xunit;

namespace BookMeta.Tests.Unit;

/// <summary>Touche à des états statiques d'Emby (BaseItem.*) : exécuté séquentiellement.</summary>
[Collection("EmbyStatics")]
public sealed class SortNameLockTests : IDisposable
{
    private readonly BookFolder _fs = new();

    public SortNameLockTests()
    {
        BaseItem.FileSystem = RealFs.FileSystem();
        BaseItem.LocalizationManager = StubProxy.Create<ILocalizationManager>(
            (m, a) => m.Name == "RemoveDiacritics" ? a[0] : m.Name == "TrimSortRemoveWords" ? a[0] : null);
    }

    public void Dispose()
    {
        _fs.Dispose();
    }

    [Fact]
    public void AvecVerrou_LeTriSurvitAApresActualisation()
    {
        var locked = new Book { Name = "Tentation" };
        SortNameLock.Apply(locked, "Meyer, Stephenie - Tentation");
        locked.AfterMetadataRefresh();
        Assert.Equal("Meyer, Stephenie - Tentation", locked.SortName);
        Assert.Contains(MetadataFields.SortName, locked.LockedFields);
    }

    [Fact]
    public void Apply_NeDuplique_PasLeVerrou_EtConserveLesAutres()
    {
        var b = new Book { Name = "T", LockedFields = new[] { MetadataFields.Cast } };
        SortNameLock.Apply(b, "A - T");
        SortNameLock.Apply(b, "A - T");
        Assert.Equal(new[] { MetadataFields.Cast, MetadataFields.SortName }, b.LockedFields);
    }

    [Fact]
    public void Apply_TriVide_NeFaitRien()
    {
        var b = new Book { Name = "T" };
        SortNameLock.Apply(b, " ");
        Assert.True(b.LockedFields == null || b.LockedFields.Length == 0);
    }

    private const string TentationSort = "Meyer, Stephenie - Tentation";

    private (BookSortNameProvider Provider, RecordingLogger Log, Book Item) Setup(string fixture = "tentation-bindery.opf", string file = "Tentation.epub")
    {
        var log = new RecordingLogger();
        var dir = _fs.Dir("Stephenie Meyer", "Tentation", 2006);
        BookFolder.PlaceSidecar(dir, fixture);
        var item = new Book { Path = BookFolder.Touch(dir, file), Name = "Tentation", InternalId = 42 };
        return (new BookSortNameProvider(log.Manager), log, item);
    }

    private static ItemUpdateType Fetch(BookSortNameProvider p, Book item) =>
        p.FetchAsync(new MetadataResult<Book> { Item = item }, new MetadataRefreshOptions(RealFs.DirectoryService()), new LibraryOptions(), CancellationToken.None)
            .GetAwaiter().GetResult();

    [Fact]
    public void CustomProvider_VerrouilleLeTriDeLInstanceRafraichie_QuiSurvitAAfterMetadataRefresh()
    {
        var (provider, log, item) = Setup();

        Assert.Equal(ItemUpdateType.MetadataEdit, Fetch(provider, item));
        item.AfterMetadataRefresh(); // ce qu'Emby fait ensuite : ne doit plus effacer le tri

        Assert.Equal(TentationSort, item.SortName);
        Assert.Contains(MetadataFields.SortName, item.LockedFields);
        Assert.Contains(log.Entries, e => e.Level == "Debug" && e.Message.Contains("tri verrouillé") && e.Message.Contains(TentationSort) && e.Message.Contains("instance"));
        Assert.False(log.HasWarning);
    }

    [Fact]
    public void CustomProvider_DeuxiemeActualisation_NeModifieRien()
    {
        var (provider, _, item) = Setup();
        Fetch(provider, item);
        Assert.Equal(ItemUpdateType.None, Fetch(provider, item));
    }

    [Fact]
    public void CustomProvider_SansOpf_NeToucheRien()
    {
        var log = new RecordingLogger();
        var dir = _fs.Dir("Auteur", "Sans opf", 2020);
        var item = new Book { Path = BookFolder.Touch(dir, "livre.pdf"), Name = "x" };
        Assert.Equal(ItemUpdateType.None, Fetch(new BookSortNameProvider(log.Manager), item));
        Assert.True(item.LockedFields == null || item.LockedFields.Length == 0);
    }

    [Fact]
    public void CustomProvider_ItemSansChemin_NeLeveRien()
    {
        var log = new RecordingLogger();
        Assert.Equal(ItemUpdateType.None, Fetch(new BookSortNameProvider(log.Manager), new Book()));
        Assert.False(log.HasWarning);
    }

    [Fact]
    public void CustomProvider_EstUnProviderForce_AuNomStable()
    {
        var p = new BookSortNameProvider(new RecordingLogger().Manager);
        Assert.IsAssignableFrom<IForcedProvider>(p);
        Assert.Equal("BookMeta Sort Name", p.Name);
    }

    [Fact]
    public void CustomProvider_LecteurBookMetaDesactive_NeFaitRien()
    {
        var (provider, log, item) = Setup();
        var disabled = new LibraryOptions { DisabledLocalMetadataReaders = new[] { "bookmeta" } };

        var r = provider.FetchAsync(new MetadataResult<Book> { Item = item }, new MetadataRefreshOptions(RealFs.DirectoryService()), disabled, CancellationToken.None)
            .GetAwaiter().GetResult();

        Assert.Equal(ItemUpdateType.None, r);
        Assert.True(item.LockedFields == null || item.LockedFields.Length == 0);
        Assert.Contains(log.Entries, e => e.Level == "Debug" && e.Message.Contains("désactivé"));
    }

    [Fact]
    public void CustomProvider_AutreLecteurDesactive_ContinueNormalement()
    {
        var (provider, _, item) = Setup();
        var other = new LibraryOptions { DisabledLocalMetadataReaders = new[] { "Nfo" } };
        var r = provider.FetchAsync(new MetadataResult<Book> { Item = item }, new MetadataRefreshOptions(RealFs.DirectoryService()), other, CancellationToken.None)
            .GetAwaiter().GetResult();
        Assert.Equal(ItemUpdateType.MetadataEdit, r);
    }
}
