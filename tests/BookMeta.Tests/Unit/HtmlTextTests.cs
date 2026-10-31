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
}
