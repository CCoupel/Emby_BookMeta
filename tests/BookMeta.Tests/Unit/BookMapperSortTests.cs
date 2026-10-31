using BookMeta.Opf;
using BookMeta.Providers;
using Xunit;

namespace BookMeta.Tests.Unit;

public class BookMapperSortTests
{
    private static OpfDocument Doc(string? author, string? fileAs, string? titleSort, string? series = null, double? index = null) => new()
    {
        Title = "Titre",
        TitleSort = titleSort,
        Creators = author == null ? Array.Empty<OpfCreator>() : new[] { new OpfCreator(author, fileAs, "aut") },
        Series = series,
        SeriesIndex = index,
    };

    [Fact] public void AuteurEtTitreDeTri() =>
        Assert.Equal("Meyer, Stephenie - Tentation", BookMapper.BuildSortName(Doc("Stephenie Meyer", "Meyer, Stephenie", "Tentation")));

    [Fact] public void SansAuteur_TitreDeTriSeul() =>
        Assert.Equal("Titre, Le", BookMapper.BuildSortName(Doc(null, null, "Titre, Le")));

    [Fact] public void SansTitreDeTri_UtiliseLeTitre() =>
        Assert.Equal("Dupont, Jean - Titre", BookMapper.BuildSortName(Doc("Jean Dupont", null, null)));

    [Fact] public void Serie_IndexSurDeuxChiffres() =>
        Assert.Equal("Meyer, Stephenie - Twilight 02 - Tentation", BookMapper.BuildSortName(Doc("Stephenie Meyer", "Meyer, Stephenie", "Tentation", "Twilight", 2)));

    [Fact] public void Serie_IndexDecimal() =>
        Assert.Equal("A, B - S 01.5 - T", BookMapper.BuildSortName(Doc("B A", "A, B", "T", "S", 1.5)));

    [Fact] public void Serie_SansIndex() =>
        Assert.Equal("A, B - S - T", BookMapper.BuildSortName(Doc("B A", "A, B", "T", "S")));

    [Theory]
    [InlineData("Stephenie Meyer", "Meyer, Stephenie")]
    [InlineData("Jean-Paul Sartre", "Sartre, Jean-Paul")]
    [InlineData("Voltaire", "Voltaire")]
    public void NomDeTriDerive(string name, string expected) =>
        Assert.Equal(expected, BookMapper.AuthorSortName(new OpfCreator(name, null, null)));

    [Theory]
    [InlineData(-1.0)]
    [InlineData(10000.0)]
    [InlineData(1e12)]
    public void IndexHorsBornes_IgnoreDansLeTri(double index) =>
        Assert.Equal("A, B - S - T", BookMapper.BuildSortName(Doc("B A", "A, B", "T", "S", index)));

    [Fact] public void IndexMaximal_EstConserve() =>
        Assert.Equal("A, B - S 9999 - T", BookMapper.BuildSortName(Doc("B A", "A, B", "T", "S", 9999)));

    [Fact] public void SerieSansAuteur() =>
        Assert.Equal("S 03 - T", BookMapper.BuildSortName(Doc(null, null, "T", "S", 3)));
}
