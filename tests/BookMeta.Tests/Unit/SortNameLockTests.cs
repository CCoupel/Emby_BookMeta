using BookMeta.Providers;
using BookMeta.Tests.Support;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
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

    [Fact]
    public void Provider_VerrouilleLeTriDeLItemReel()
    {
        var log = new RecordingLogger();
        var dir = _fs.Dir("Stephenie Meyer", "Tentation", 2006);
        BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");
        var path = BookFolder.Touch(dir, "Tentation.epub");

        var real = new Book { Path = path, Name = "Tentation", InternalId = 42 };

        var provider = new BookMeta.Providers.BookMetadataProvider(log.Manager, RealFs.FileSystem())
        {
            ItemLookup = id => id == 42 ? real : null,
        };
        var result = provider.GetMetadata(new ItemInfo(real), new MediaBrowser.Model.Configuration.LibraryOptions(),
            RealFs.DirectoryService(), CancellationToken.None).GetAwaiter().GetResult();

        Assert.True(result.HasMetadata);
        Assert.Equal("Meyer, Stephenie - Tentation", real.SortName);
        real.AfterMetadataRefresh();
        Assert.Equal("Meyer, Stephenie - Tentation", real.SortName);
        Assert.False(log.HasWarning);
    }

    [Fact]
    public void Provider_ItemReelIntrouvable_NeLeveRien()
    {
        var log = new RecordingLogger();
        var dir = _fs.Dir("Stephenie Meyer", "Tentation", 2006);
        BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");
        var real = new Book { Path = BookFolder.Touch(dir, "Tentation.epub"), Name = "Tentation", InternalId = 42 };

        var provider = new BookMeta.Providers.BookMetadataProvider(log.Manager, RealFs.FileSystem()) { ItemLookup = _ => null };
        var result = provider.GetMetadata(new ItemInfo(real), new MediaBrowser.Model.Configuration.LibraryOptions(),
            RealFs.DirectoryService(), CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(result.HasMetadata);
        Assert.False(log.HasWarning);
    }

    [Fact]
    public void Provider_JournaliseEnDebug_LeVerrouEtSesRefus()
    {
        var dir = _fs.Dir("Stephenie Meyer", "Tentation", 2006);
        BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");
        var real = new Book { Path = BookFolder.Touch(dir, "Tentation.epub"), Name = "Tentation", InternalId = 42 };
        string Run(Func<long, BaseItem?> lookup)
        {
            var log = new RecordingLogger();
            new BookMeta.Providers.BookMetadataProvider(log.Manager, RealFs.FileSystem()) { ItemLookup = lookup }
                .GetMetadata(new ItemInfo(real), new MediaBrowser.Model.Configuration.LibraryOptions(), RealFs.DirectoryService(), CancellationToken.None)
                .GetAwaiter().GetResult();
            return string.Join("\n", log.Entries.Where(e => e.Level == "Debug").Select(e => e.Message));
        }

        Assert.Contains("tri verrouillé pour", Run(_ => real));
        Assert.Contains("Meyer, Stephenie - Tentation", Run(_ => real));
        Assert.Contains("introuvable", Run(_ => null));
        Assert.Contains("pas un livre", Run(_ => new MediaBrowser.Controller.Entities.Audio.Audio()));
    }
}
