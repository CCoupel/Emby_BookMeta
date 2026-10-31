using System.Text;
using BookMeta.Opf;
using Xunit;

namespace BookMeta.Tests.Unit;

public class OpfParserTests
{
    private const string Opf2 = @"<?xml version='1.0' encoding='utf-8'?>
<package xmlns='http://www.idpf.org/2007/opf' version='2.0' unique-identifier='uid'>
 <metadata xmlns:dc='http://purl.org/dc/elements/1.1/' xmlns:opf='http://www.idpf.org/2007/opf'>
  <dc:title>Tentation</dc:title>
  <dc:creator opf:role='aut' opf:file-as='Meyer, Stephenie'>Stephenie Meyer</dc:creator>
  <dc:creator opf:role='trl'>Lyonel Lorsin</dc:creator>
  <dc:description>L&amp;#39;été &lt;b&gt;noir&lt;/b&gt;</dc:description>
  <dc:date>2006-10-02T00:00:00+00:00</dc:date>
  <dc:language>fre</dc:language>
  <dc:publisher>Hachette</dc:publisher><dc:publisher>hachette</dc:publisher>
  <dc:subject>Roman</dc:subject><dc:subject>ROMAN</dc:subject><dc:subject>Fantastique</dc:subject>
  <dc:identifier opf:scheme='ISBN'>9782012012345</dc:identifier>
  <dc:identifier opf:scheme='BNF'>cb43546026q</dc:identifier>
  <dc:identifier>urn:isbn:978-2-01-201234-5</dc:identifier>
  <meta name='calibre:title_sort' content='Tentation'/>
  <meta name='calibre:series' content='Twilight'/>
  <meta name='calibre:series_index' content='2.0'/>
  <meta name='cover' content='cov'/>
 </metadata>
 <manifest>
  <item id='t' href='text.xhtml' media-type='application/xhtml+xml'/>
  <item id='cov' href='images/c%C3%B4ver.jpg' media-type='image/jpeg'/>
  <item id='other' href='x.png' media-type='image/png'/>
 </manifest>
</package>";

    private const string Opf3 = @"<package xmlns='http://www.idpf.org/2007/opf' version='3.0'>
 <metadata xmlns:dc='http://purl.org/dc/elements/1.1/'>
  <dc:title id='t1'>La psy</dc:title>
  <meta refines='#t1' property='file-as'>Psy (La)</meta>
  <dc:creator id='a1'>Freida McFadden</dc:creator>
  <meta refines='#a1' property='role' scheme='marc:relators'>aut</meta>
  <meta refines='#a1' property='file-as'>McFadden, Freida</meta>
  <dc:identifier id='i1'>9782253000000</dc:identifier>
  <meta refines='#i1' property='identifier-type'>ISBN</meta>
  <dc:date>2023-05</dc:date>
  <meta property='belongs-to-collection' id='c1'>Les Psy</meta>
  <meta refines='#c1' property='collection-type'>series</meta>
  <meta refines='#c1' property='group-position'>3</meta>
 </metadata>
 <manifest>
  <item id='a' href='a.jpg' media-type='image/jpeg'/>
  <item id='b' href='b.jpg' media-type='image/jpeg' properties='cover-image'/>
 </manifest>
</package>";

    private static OpfDocument Parse(string xml)
    {
        Assert.True(OpfParser.Parse(xml, out var doc));
        return doc!;
    }

    [Fact]
    public void Opf2_ChampsPrincipaux()
    {
        var d = Parse(Opf2);
        Assert.Equal("Tentation", d.Title);
        Assert.Equal("Tentation", d.TitleSort);
        Assert.Equal(2, d.Creators.Count);
        var a = Assert.Single(d.Authors);
        Assert.Equal(new OpfCreator("Stephenie Meyer", "Meyer, Stephenie", "aut"), a);
        Assert.Equal("L'été noir", d.Description);
        Assert.Equal(new DateTimeOffset(2006, 10, 2, 0, 0, 0, TimeSpan.Zero), d.Date);
        Assert.Equal("fre", d.Language);
        Assert.Equal(new[] { "Hachette" }, d.Publishers);
        Assert.Equal(new[] { "Roman", "Fantastique" }, d.Subjects);
        Assert.Equal("Twilight", d.Series);
        Assert.Equal(2.0, d.SeriesIndex);
    }

    [Fact]
    public void Opf2_Identifiants()
    {
        var d = Parse(Opf2);
        Assert.Contains(new OpfIdentifier("ISBN", "9782012012345"), d.Identifiers);
        Assert.Contains(new OpfIdentifier("BNF", "cb43546026q"), d.Identifiers);
        Assert.Contains(new OpfIdentifier("ISBN", "978-2-01-201234-5"), d.Identifiers); // urn:isbn:
    }

    [Fact]
    public void Opf2_Couverture_ParMetaCover()
    {
        var d = Parse(Opf2);
        Assert.Equal("cov", d.CoverId);
        Assert.Equal("images/c%C3%B4ver.jpg", d.FindCoverItem()!.Href);
    }

    [Fact]
    public void Opf3_Raffinements_Serie_Identifiant()
    {
        var d = Parse(Opf3);
        Assert.Equal("La psy", d.Title);
        Assert.Equal("Psy (La)", d.TitleSort);
        var a = Assert.Single(d.Authors);
        Assert.Equal(new OpfCreator("Freida McFadden", "McFadden, Freida", "aut"), a);
        Assert.Equal("Les Psy", d.Series);
        Assert.Equal(3, d.SeriesIndex);
        Assert.Contains(new OpfIdentifier("ISBN", "9782253000000"), d.Identifiers);
        Assert.Equal(new DateTimeOffset(2023, 5, 1, 0, 0, 0, TimeSpan.Zero), d.Date);
    }

    [Fact]
    public void Opf3_Couverture_ParCoverImage() =>
        Assert.Equal("b.jpg", Parse(Opf3).FindCoverItem()!.Href);

    [Fact]
    public void Couverture_SansIndice_PremiereImage()
    {
        var d = Parse("<package xmlns='http://www.idpf.org/2007/opf'><metadata/><manifest><item id='x' href='t.xhtml' media-type='application/xhtml+xml'/><item id='y' href='p.png' media-type='image/png'/></manifest></package>");
        Assert.Equal("p.png", d.FindCoverItem()!.Href);
    }

    [Fact]
    public void SansNamespaceNiPrefixe_EstTolere()
    {
        var d = Parse("<package><metadata><title>T</title><creator role='aut'>A</creator><meta name='calibre:series' content='S'/></metadata></package>");
        Assert.Equal("T", d.Title);
        Assert.Equal("A", Assert.Single(d.Authors).Name);
        Assert.Equal("S", d.Series);
    }

    [Fact]
    public void ChampsAbsents_RestentNuls()
    {
        var d = Parse("<package xmlns='http://www.idpf.org/2007/opf'><metadata xmlns:dc='http://purl.org/dc/elements/1.1/'><dc:title>  </dc:title></metadata></package>");
        Assert.Null(d.Title);
        Assert.Null(d.Date);
        Assert.Null(d.Series);
        Assert.Empty(d.Creators);
        Assert.Null(d.FindCoverItem());
    }

    [Theory]
    [InlineData("2006-10-02", 2006, 10, 2)]
    [InlineData("2006-10", 2006, 10, 1)]
    [InlineData("2006", 2006, 1, 1)]
    [InlineData("2006-10-02T08:30:00+02:00", 2006, 10, 2)]
    public void Dates_Valides(string s, int y, int m, int d)
    {
        var r = OpfParser.ParseDate(s);
        Assert.NotNull(r);
        Assert.Equal((y, m, d), (r!.Value.Year, r.Value.Month, r.Value.Day));
    }

    [Theory]
    [InlineData("")]
    [InlineData("demain")]
    [InlineData("0101-01-01T00:00:00+00:00")]
    public void Dates_Invalides_Ignorees(string s) => Assert.Null(OpfParser.ParseDate(s));

    [Theory]
    [InlineData("pas du xml")]
    [InlineData("<package><metadata>")]
    [InlineData("<html/>")]
    [InlineData("<package/>")]
    public void OpfInvalide_RetourneFalse_SansException(string xml)
    {
        Assert.False(OpfParser.Parse(xml, out var doc));
        Assert.Null(doc);
    }

    [Fact]
    public void Dtd_EstIgnoree_EntiteExterneNonResolue()
    {
        var xml = "<?xml version='1.0'?><!DOCTYPE package [<!ENTITY x SYSTEM 'file:///etc/passwd'>]>" +
                  "<package xmlns='http://www.idpf.org/2007/opf'><metadata xmlns:dc='http://purl.org/dc/elements/1.1/'><dc:title>T&x;</dc:title></metadata></package>";
        var ok = OpfParser.Parse(xml, out var doc);
        if (ok) Assert.DoesNotContain("root:", doc!.Title);
    }

    [Fact]
    public void EntreeTropGrosse_EstRefusee()
    {
        using var big = new MemoryStream(new byte[OpfParser.MaxBytes + 10]);
        Assert.False(OpfParser.Parse(big, out var doc, out var err));
        Assert.Null(doc);
        Assert.NotNull(err);
    }

    [Fact]
    public void Utf8AvecBom_EstLu()
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(Opf2)).ToArray();
        using var ms = new MemoryStream(bytes);
        Assert.True(OpfParser.Parse(ms, out var doc));
        Assert.Equal("Tentation", doc!.Title);
    }
}
