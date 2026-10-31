namespace BookMeta.Opf;

/// <summary>Auteur (ou autre contributeur) d'un OPF.</summary>
public sealed record OpfCreator(string Name, string? FileAs, string? Role);

/// <summary>Identifiant d'un OPF ; <see cref="Scheme"/> en majuscules (ISBN, BNF, BINDERY…), éventuellement vide.</summary>
public sealed record OpfIdentifier(string Scheme, string Value);

/// <summary>Entrée du manifest (<c>item</c>) d'un OPF.</summary>
public sealed record OpfManifestItem(string? Id, string Href, string? MediaType, string? Properties)
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    /// <summary><c>media-type</c> image/*, ou (media-type absent) extension d'image connue.</summary>
    public bool IsImage => MediaType != null
        ? MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        : ImageExtensions.Contains(Path.GetExtension(Href), StringComparer.OrdinalIgnoreCase);

    public bool HasProperty(string property) =>
        Properties != null && Properties.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(property, StringComparer.OrdinalIgnoreCase);
}

/// <summary>Modèle immuable des informations lues dans un OPF (2.0 ou 3.x). Champ absent = <c>null</c> / liste vide.</summary>
public sealed class OpfDocument
{
    public string? Title { get; init; }
    public string? TitleSort { get; init; }
    public IReadOnlyList<OpfCreator> Creators { get; init; } = Array.Empty<OpfCreator>();
    /// <summary>Texte brut de <c>dc:description</c> (voir <see cref="HtmlText"/>).</summary>
    public string? Description { get; init; }
    public DateTimeOffset? Date { get; init; }
    public string? Language { get; init; }
    public IReadOnlyList<string> Publishers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Subjects { get; init; } = Array.Empty<string>();
    public string? Series { get; init; }
    public double? SeriesIndex { get; init; }
    public IReadOnlyList<OpfIdentifier> Identifiers { get; init; } = Array.Empty<OpfIdentifier>();
    public IReadOnlyList<OpfManifestItem> Manifest { get; init; } = Array.Empty<OpfManifestItem>();
    /// <summary>OPF 2 : <c>&lt;meta name="cover" content="id"/&gt;</c>.</summary>
    public string? CoverId { get; init; }

    /// <summary>Auteurs : <c>dc:creator</c> sans rôle ou de rôle <c>aut</c>.</summary>
    public IEnumerable<OpfCreator> Authors => Creators.Where(c =>
        string.IsNullOrWhiteSpace(c.Role) || string.Equals(c.Role.Trim(), "aut", StringComparison.OrdinalIgnoreCase));

    /// <summary>Candidates à la couverture par priorité : OPF 2 (<c>meta cover</c>), OPF 3 (<c>cover-image</c>), puis les images du manifest dans l'ordre. Images seulement.</summary>
    public IEnumerable<OpfManifestItem> CoverCandidates()
    {
        var seen = new HashSet<OpfManifestItem>();
        if (!string.IsNullOrEmpty(CoverId))
            foreach (var m in Manifest.Where(m => string.Equals(m.Id, CoverId, StringComparison.Ordinal) && m.IsImage))
                if (seen.Add(m)) yield return m;
        foreach (var m in Manifest.Where(m => m.HasProperty("cover-image") && m.IsImage))
            if (seen.Add(m)) yield return m;
        foreach (var m in Manifest.Where(m => m.IsImage))
            if (seen.Add(m)) yield return m;
    }

    /// <summary>Première candidate de <see cref="CoverCandidates"/>.</summary>
    public OpfManifestItem? FindCoverItem() => CoverCandidates().FirstOrDefault();
}
