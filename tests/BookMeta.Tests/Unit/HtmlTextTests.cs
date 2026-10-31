using BookMeta.Opf;
using Xunit;

namespace BookMeta.Tests.Unit;

public class HtmlTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p></p>")]
    public void Vide_DonneNull(string? input) => Assert.Null(HtmlText.ToPlainText(input));

    [Fact]
    public void Entites_SontDecodees() =>
        Assert.Equal("L'été d'Édouard & Bella", HtmlText.ToPlainText("L&#39;&eacute;t&eacute; d&#39;&Eacute;douard &amp; Bella"));

    [Fact]
    public void BrEtParagraphes_DonnentDesSautsDeLigne() =>
        Assert.Equal("Un\nDeux\n\nTrois", HtmlText.ToPlainText("<p>Un<br/>Deux</p><p>Trois</p>")!.Replace("\n\n\n", "\n\n"));

    [Fact]
    public void Balises_SontRetirees() =>
        Assert.Equal("gras et italique", HtmlText.ToPlainText("<b>gras</b> et <i class=\"x\">italique</i>"));

    [Fact]
    public void BalisageDoubleEchappe_EstTraite() =>
        Assert.Equal("Salut", HtmlText.ToPlainText("&lt;p&gt;Salut&lt;/p&gt;"));

    [Fact]
    public void Blancs_SontNormalises() =>
        Assert.Equal("a b\nc", HtmlText.ToPlainText("  a \t  b  \r\n  c  "));

    [Fact]
    public void ComparaisonLitterale_NEstPasPriseCommeBalise() =>
        Assert.Equal("si a < b alors", HtmlText.ToPlainText("si a < b alors"));

    [Fact]
    public void EntreePathologique_EstTraiteeRapidement()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var r = HtmlText.ToPlainText(string.Concat(Enumerable.Repeat("<a", 500_000)) + " fin", out _);
        sw.Stop();
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1), $"trop lent : {sw.Elapsed}");
        Assert.NotNull(r);
    }

    [Fact]
    public void ChevronsOuvrantsEtEspaces_EnRafale_SontRapides()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        HtmlText.ToPlainText(string.Concat(Enumerable.Repeat("<" + new string(' ', 200), 400)), out _);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Description_EstPlafonnee()
    {
        var r = HtmlText.ToPlainText(new string('x', HtmlText.MaxLength * 3));
        Assert.Equal(HtmlText.MaxLength, r!.Length);
    }
}
