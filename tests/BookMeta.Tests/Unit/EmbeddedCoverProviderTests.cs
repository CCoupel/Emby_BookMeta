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
}
