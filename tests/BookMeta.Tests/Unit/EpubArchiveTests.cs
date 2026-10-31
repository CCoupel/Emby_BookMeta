using System.IO.Compression;
using System.Text;
using BookMeta.Epub;
using BookMeta.Opf;
using Xunit;

namespace BookMeta.Tests.Unit;

public class EpubArchiveTests
{
    private const string Container = "<?xml version='1.0'?><container version='1.0' xmlns='urn:oasis:names:tc:opendocument:xmlns:container'><rootfiles><rootfile full-path='OEBPS/content.opf' media-type='application/oebps-package+xml'/></rootfiles></container>";
    private const string Opf = "<package xmlns='http://www.idpf.org/2007/opf'><metadata xmlns:dc='http://purl.org/dc/elements/1.1/'><dc:title>Dans l'epub</dc:title></metadata></package>";

    private static Stream Zip(params (string name, string content)[] entries)
    {
        var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, content) in entries)
            {
                using var w = z.CreateEntry(name).Open();
                var b = Encoding.UTF8.GetBytes(content);
                w.Write(b, 0, b.Length);
            }
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Rootfile_EtOpf_SontLus()
    {
        using var epub = new EpubArchive(Zip(("META-INF/container.xml", Container), ("OEBPS/content.opf", Opf)));
        Assert.Equal("OEBPS/content.opf", epub.RootfilePath);
        using var s = epub.OpenOpf();
        Assert.NotNull(s);
        Assert.True(OpfParser.Parse(s!, out var doc));
        Assert.Equal("Dans l'epub", doc!.Title);
    }

    [Fact]
    public void SansContainer_RootfileNul()
    {
        using var epub = new EpubArchive(Zip(("a.txt", "x")));
        Assert.Null(epub.RootfilePath);
        Assert.Null(epub.OpenOpf());
    }

    [Fact]
    public void ContainerSansRootfile_RootfileNul()
    {
        using var epub = new EpubArchive(Zip(("META-INF/container.xml", "<container><rootfiles/></container>")));
        Assert.Null(epub.RootfilePath);
    }

    [Fact]
    public void ContainerMalForme_RootfileNul_SansException()
    {
        using var epub = new EpubArchive(Zip(("META-INF/container.xml", "<container")));
        Assert.Null(epub.RootfilePath);
    }

    [Fact]
    public void Entree_InsensibleALaCasse_EtContenuSurvitAuDispose()
    {
        Stream? s;
        using (var epub = new EpubArchive(Zip(("OEBPS/Images/Cover.JPG", "abc"))))
        {
            Assert.True(epub.HasEntry("oebps/images/cover.jpg"));
            s = epub.OpenEntry("oebps/images/cover.jpg");
        }
        Assert.Equal("abc", new StreamReader(s!).ReadToEnd());
    }

    [Fact]
    public void EntreeAbsente_Nulle()
    {
        using var epub = new EpubArchive(Zip(("a", "x")));
        Assert.Null(epub.OpenEntry("nope"));
    }

    [Theory]
    [InlineData("OEBPS/content.opf", "images/cover.jpg", "OEBPS/images/cover.jpg")]
    [InlineData("OEBPS/content.opf", "../images/cover.jpg", "images/cover.jpg")]
    [InlineData("content.opf", "cover.jpg", "cover.jpg")]
    [InlineData("OEBPS/content.opf", "images/c%C3%B4ver.jpg#frag", "OEBPS/images/côver.jpg")]
    [InlineData("OEBPS/content.opf", "./a/./b.png?x=1", "OEBPS/a/b.png")]
    [InlineData("OEBPS/content.opf", "/abs/c.jpg", "abs/c.jpg")]
    [InlineData("OEBPS\\content.opf", "i\\c.jpg", "OEBPS/i/c.jpg")]
    public void ResolveHref_Valide(string opf, string href, string attendu) =>
        Assert.Equal(attendu, EpubArchive.ResolveHref(opf, href));

    [Theory]
    [InlineData("content.opf", "../../x.jpg")]
    [InlineData("OEBPS/content.opf", "http://exemple.org/c.jpg")]
    [InlineData("OEBPS/content.opf", "data:image/png;base64,AAAA")]
    [InlineData("OEBPS/content.opf", "")]
    [InlineData("OEBPS/content.opf", "#seul")]
    public void ResolveHref_Refuse(string opf, string href) => Assert.Null(EpubArchive.ResolveHref(opf, href));

    [Fact]
    public void Fichier_NonZip_TryOpenFalse()
    {
        var dir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "tmp-" + Guid.NewGuid().ToString("N")));
        try
        {
            var p = Path.Combine(dir.FullName, "x.epub");
            File.WriteAllText(p, "ceci n'est pas un zip");
            Assert.False(EpubArchive.TryOpen(p, out var a, out var err));
            Assert.Null(a);
            Assert.NotNull(err);
            Assert.False(EpubArchive.TryOpen(Path.Combine(dir.FullName, "absent.epub"), out _, out _));
        }
        finally { dir.Delete(true); }
    }

    [Fact]
    public void OpenEntry_RespecteMaxBytes()
    {
        using var epub = new EpubArchive(Zip(("big.bin", new string('a', 2000))));
        Assert.Null(epub.OpenEntry("big.bin", 1000));
        Assert.NotNull(epub.OpenEntry("big.bin", 4000));
    }
}
