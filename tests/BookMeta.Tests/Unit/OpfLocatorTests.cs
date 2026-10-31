using System.IO.Compression;
using System.Text;
using BookMeta.Opf;
using Xunit;

namespace BookMeta.Tests.Unit;

public sealed class OpfLocatorTests : IDisposable
{
    private readonly DirectoryInfo _dir = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "tmp-" + Guid.NewGuid().ToString("N")));
    public void Dispose() => _dir.Delete(true);

    private const string Opf = "<package xmlns='http://www.idpf.org/2007/opf'><metadata xmlns:dc='http://purl.org/dc/elements/1.1/'><dc:title>{0}</dc:title></metadata></package>";

    private string Write(string name, string text) { var p = Path.Combine(_dir.FullName, name); File.WriteAllText(p, text); return p; }

    private string WriteEpub(string name, string title)
    {
        var p = Path.Combine(_dir.FullName, name);
        using var z = ZipFile.Open(p, ZipArchiveMode.Create);
        void Add(string n, string c) { using var w = z.CreateEntry(n).Open(); var b = Encoding.UTF8.GetBytes(c); w.Write(b, 0, b.Length); }
        Add("META-INF/container.xml", "<container><rootfiles><rootfile full-path='c.opf'/></rootfiles></container>");
        Add("c.opf", string.Format(Opf, title));
        return p;
    }

    [Fact]
    public void Sidecar_EstPrioritaire_NomInsensibleALaCasse()
    {
        var book = WriteEpub("livre.epub", "Interne");
        Write("Metadata.OPF", string.Format(Opf, "Sidecar"));
        var r = OpfLocator.Locate(book, _dir.FullName, null, null);
        Assert.Equal("Sidecar", r!.Document.Title);
        Assert.Equal(OpfSourceKind.Sidecar, r.Kind);
    }

    [Fact]
    public void SansSidecar_RepliSurEpub()
    {
        var book = WriteEpub("livre.epub", "Interne");
        var r = OpfLocator.Locate(book, null, null, null);
        Assert.Equal("Interne", r!.Document.Title);
        Assert.Equal(OpfSourceKind.EpubInternal, r.Kind);
        Assert.Equal(book, r.SourcePath);
    }

    [Fact]
    public void SidecarIllisible_RepliSurEpub()
    {
        var book = WriteEpub("livre.epub", "Interne");
        Write("metadata.opf", "<<< pas du xml");
        Assert.Equal("Interne", OpfLocator.Locate(book, _dir.FullName, null, null)!.Document.Title);
    }

    [Fact]
    public void CbzOuPdfSansSidecar_Nul()
    {
        var cbz = Write("b.cbz", "zip?");
        Assert.Null(OpfLocator.Locate(cbz, _dir.FullName, null, null));
    }

    [Fact]
    public void EpubCorrompuSansSidecar_Nul_SansException()
    {
        var bad = Write("x.epub", "corrompu");
        Assert.Null(OpfLocator.Locate(bad, _dir.FullName, null, null));
    }

    [Fact]
    public void ListeurInjecte_EstUtilise()
    {
        var book = Write("a.pdf", "x");
        var opf = Write("metadata.opf", string.Format(Opf, "Via liste"));
        var r = OpfLocator.Locate(book, _dir.FullName, _ => new[] { opf }, null);
        Assert.Equal("Via liste", r!.Document.Title);
    }

    [Fact]
    public void ListeurQuiLeve_NeRemonteAucuneException() =>
        Assert.Null(OpfLocator.Locate("/x/y.pdf", "/x", _ => throw new IOException("boum"), null));
}
