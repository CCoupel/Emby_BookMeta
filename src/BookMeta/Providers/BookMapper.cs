using System.Globalization;
using BookMeta.Opf;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace BookMeta.Providers;

/// <summary>OpfDocument → <see cref="Book"/> (contrat <c>contracts/models.md</c> §2). Pur : aucune E/S.</summary>
public static class BookMapper
{
    public const string SeriesTagPrefix = "Série : ";
    public const string ProviderName = "BookMeta";

    /// <summary>Retourne <c>null</c> si l'OPF n'a pas de titre (rien à renseigner).</summary>
    public static Book? Map(OpfDocument opf)
    {
        if (string.IsNullOrWhiteSpace(opf.Title)) return null;

        var book = new Book { Name = opf.Title };
        book.SortName = BuildSortName(opf);
        if (opf.Description != null) book.Overview = opf.Description;
        if (opf.Date is { } d)
        {
            book.PremiereDate = d;
            book.ProductionYear = d.Year;
        }
        if (opf.Publishers.Count > 0) book.SetStudios(opf.Publishers);
        foreach (var subject in opf.Subjects) book.AddGenre(subject);

        var tags = new List<string>();
        void AddTag(string t) { if (!tags.Contains(t, StringComparer.OrdinalIgnoreCase)) tags.Add(t); }
        foreach (var a in opf.Authors) AddTag(a.Name);
        if (!string.IsNullOrWhiteSpace(opf.Series)) AddTag(SeriesTagPrefix + opf.Series);
        if (tags.Count > 0) book.Tags = tags.ToArray();

        foreach (var id in opf.Identifiers)
        {
            switch (id.Scheme)
            {
                case "ISBN":
                    var isbn = new string(id.Value.Where(c => char.IsLetterOrDigit(c)).ToArray());
                    if (isbn.Length > 0) book.SetProviderId(MetadataProviders.ISBN, isbn);
                    break;
                case "BNF":
                case "BINDERY":
                    book.SetProviderId(id.Scheme, id.Value);
                    break;
            }
        }
        return book;
    }

    /// <summary>
    /// <c>"&lt;Nom, Prénom&gt; - &lt;titre de tri&gt;"</c> ; avec série : <c>"&lt;Nom, Prénom&gt; - &lt;Série&gt; NN - &lt;titre de tri&gt;"</c> ;
    /// sans auteur : titre de tri seul ; sans titre de tri : titre.
    /// </summary>
    public static string BuildSortName(OpfDocument opf)
    {
        var titleSort = !string.IsNullOrWhiteSpace(opf.TitleSort) ? opf.TitleSort! : opf.Title ?? string.Empty;
        var author = opf.Authors.FirstOrDefault();
        var authorSort = author == null ? null : AuthorSortName(author);
        var parts = new List<string>();
        if (authorSort != null) parts.Add(authorSort);
        if (!string.IsNullOrWhiteSpace(opf.Series))
        {
            var series = opf.Series!;
            parts.Add(opf.SeriesIndex is { } i && i >= 0 && i <= 9999 ? series + " " + FormatIndex(i) : series);
        }
        parts.Add(titleSort);
        return parts.Count == 1 ? titleSort : string.Join(" - ", parts);
    }

    /// <summary><c>file-as</c> de l'auteur, sinon « Nom, Prénom » dérivé (dernier mot = nom).</summary>
    public static string AuthorSortName(OpfCreator author)
    {
        if (!string.IsNullOrWhiteSpace(author.FileAs)) return author.FileAs!.Trim();
        var name = author.Name.Trim();
        var cut = name.LastIndexOf(' ');
        return cut <= 0 ? name : name.Substring(cut + 1) + ", " + name.Substring(0, cut).Trim();
    }

    private static string FormatIndex(double index)
    {
        var whole = Math.Floor(index);
        var s = ((int)whole).ToString("00", CultureInfo.InvariantCulture);
        var frac = index - whole;
        if (frac > 1e-9) s += frac.ToString("0.##", CultureInfo.InvariantCulture).Substring(1); // ".5"
        return s;
    }
}
