using BookMeta.Providers;
using BookMeta.Tests.Support;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Xunit;

namespace BookMeta.Tests.Unit;

public class EmbeddedCoverProviderTests
{
    [Fact]
    public void ResultatSansPath_ReponseVide_SansException()
    {
        var log = new RecordingLogger();
        var provider = new BookEmbeddedCoverProvider(log.Manager, RealFs.FileSystem());
        var result = new MetadataResult<Book> { Item = new Book { Name = "sans chemin" } };

        var r = provider.GetImage(result, ImageType.Primary, CancellationToken.None).GetAwaiter().GetResult();

        Assert.NotNull(r);
        Assert.Null(r.Stream);
        Assert.True(string.IsNullOrEmpty(r.Path));
        Assert.False(log.HasWarning);
    }

    [Fact]
    public void LivreFactice_SansChemin_EstSupporte_PourQueLeProviderSoitPropose()
    {
        // Emby liste les récupérateurs d'images d'une bibliothèque avec un livre sans Path (défaut D2 de la recette emby2).
        var provider = new BookEmbeddedCoverProvider(new RecordingLogger().Manager, RealFs.FileSystem());
        Assert.True(provider.Supports(new Book()));
        Assert.False(provider.Supports(new MediaBrowser.Controller.Entities.Audio.Audio()));
    }

    [Theory]
    [InlineData("/x/a.cbz", true)]
    [InlineData("/x/a.epub", true)]
    [InlineData("/x/a.pdf", false)]
    public void AvecChemin_LExtensionDecide(string path, bool expected)
    {
        var provider = new BookEmbeddedCoverProvider(new RecordingLogger().Manager, RealFs.FileSystem());
        Assert.Equal(expected, provider.Supports(new Book { Path = path }));
    }

    [Fact]
    public void OptionEpubDesactivee_ExclutLesEpubSeulement()
    {
        var provider = new BookEmbeddedCoverProvider(new RecordingLogger().Manager, RealFs.FileSystem()) { EpubExtractionEnabled = () => false };
        Assert.False(provider.Supports(new Book { Path = "/x/a.epub" }));
        Assert.True(provider.Supports(new Book { Path = "/x/a.cbz" }));
    }

    [Theory]
    [InlineData("/config/metadata/dummy")]   // livre factice de ProviderManager.GetPluginSummary : chemin sans extension (défaut D2-bis)
    [InlineData("C:\\emby\\metadata\\dummy")]
    public void LivreFacticeDeLaListeDesReglages_ExtensionVide_EstSupporte(string path)
    {
        var provider = new BookEmbeddedCoverProvider(new RecordingLogger().Manager, RealFs.FileSystem());
        Assert.True(provider.Supports(new Book { Path = path }));
    }
}
