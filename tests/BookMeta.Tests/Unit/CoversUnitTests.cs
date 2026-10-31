using System.IO.Compression;
using System.Text;
using BookMeta.Covers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using Xunit;

namespace BookMeta.Tests.Unit;

public sealed class CoversUnitTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "tmp-" + Guid.NewGuid().ToString("N")));
    public void Dispose() => _dir.Delete(true);

    private static FileSystemMetadata F(string name) => new() { Name = name, FullName = "/x/" + name, Exists = true };

    [Fact]
    public void CoverPrimeSurFolder_EtJpgSurPng()
    {
        Assert.Equal("cover.png", CoverFileLocator.Find(new[] { F("folder.jpg"), F("cover.png") })!.Name);
        Assert.Equal("Cover.JPG", CoverFileLocator.Find(new[] { F("cover.png"), F("Cover.JPG") })!.Name);
        Assert.Equal("FOLDER.webp", CoverFileLocator.Find(new[] { F("a.txt"), F("FOLDER.webp") })!.Name);
        Assert.Null(CoverFileLocator.Find(new[] { F("cover.gif"), F("cover.txt"), F("covers.jpg") }));
    }

    private string Cbz(params string[] names)
    {
        var p = Path.Combine(_dir.FullName, Guid.NewGuid().ToString("N") + ".cbz");
        using var z = ZipFile.Open(p, ZipArchiveMode.Create);
        foreach (var n in names)
        {
            using var w = z.CreateEntry(n).Open();
            var b = Encoding.ASCII.GetBytes(n);
            w.Write(b, 0, b.Length);
        }
        return p;
    }

    private static string Read(CoverImage c) => new StreamReader(c.Data).ReadToEnd();

    [Fact]
    public void Cbz_OrdreNaturel_ExclutMacosxEtCaches()
    {
        var c = CbzCoverExtractor.Extract(Cbz("__MACOSX/page1.jpg", ".hidden.jpg", "p10.jpg", "p2.jpg", "p1.png", "notes.txt"), null);
        Assert.Equal("p1.png", Read(c!));
        Assert.Equal(ImageFormat.Png, c!.Format);
    }

    [Fact]
    public void Cbz_NaturelAvecSousDossier()
    {
        var c = CbzCoverExtractor.Extract(Cbz("ch/page10.jpg", "ch/page9.jpg"), null);
        Assert.Equal("ch/page9.jpg", Read(c!));
    }

    [Fact]
    public void Cbz_SansImage_OuCorrompu_Null()
    {
        Assert.Null(CbzCoverExtractor.Extract(Cbz("a.txt"), null));
        var bad = Path.Combine(_dir.FullName, "bad.cbz");
        File.WriteAllText(bad, "pas un zip");
        Assert.Null(CbzCoverExtractor.Extract(bad, null));
        Assert.Null(CbzCoverExtractor.Extract(Path.Combine(_dir.FullName, "absent.cbz"), null));
    }

    [Theory]
    [InlineData("image/jpeg", ImageFormat.Jpg)]
    [InlineData("IMAGE/PNG", ImageFormat.Png)]
    [InlineData("image/webp", ImageFormat.Webp)]
    [InlineData("image/gif", ImageFormat.Gif)]
    public void FormatsParMediaType(string mt, ImageFormat expected) => Assert.Equal(expected, ImageFormats.FromMediaType(mt));

    [Fact]
    public void FormatInconnu_Null()
    {
        Assert.Null(ImageFormats.FromMediaType("image/svg+xml"));
        Assert.Null(ImageFormats.FromExtension(".pdf"));
    }
}
