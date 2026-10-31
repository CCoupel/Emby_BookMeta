using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;

namespace BookMeta.Startup;

/// <summary>Accès aux bibliothèques de livres du serveur (abstrait pour les tests).</summary>
public interface IBookLibraryStore
{
    /// <summary>Bibliothèques dont le type de contenu est « books ».</summary>
    IReadOnlyList<BookLibrary> GetBookLibraries();

    /// <summary>Enregistre les options (API serveur prévue pour les options de bibliothèque).</summary>
    void Save(BookLibrary library);
}

/// <summary>Une bibliothèque de livres et ses options (modifiées en place puis enregistrées).</summary>
public sealed record BookLibrary(string Id, string Name, LibraryOptions Options);

/// <summary>
/// Active « BookMeta Embedded Cover » dans <c>TypeOptions[Book].ImageFetchers</c> des bibliothèques de livres, une seule fois par bibliothèque
/// (identifiants mémorisés dans <paramref name="processedIds"/>). Ne touche à aucune autre option ; ne lève jamais.
/// </summary>
public static class BookLibraryActivator
{
    public const string BookType = "Book";

    /// <returns>Nombre de bibliothèques activées lors de cet appel.</returns>
    public static int Run(IBookLibraryStore store, string fetcherName, IList<string> processedIds, Action persistProcessedIds, ILogger logger)
    {
        var activated = 0;
        try
        {
            foreach (var library in store.GetBookLibraries())
            {
                try
                {
                    if (processedIds.Contains(library.Id, StringComparer.OrdinalIgnoreCase)) continue;

                    var typeOptions = FindOrCreateBookOptions(library.Options, out var created);
                    var changed = created;
                    if (!typeOptions.ImageFetchers.Contains(fetcherName, StringComparer.OrdinalIgnoreCase))
                    {
                        typeOptions.ImageFetchers = typeOptions.ImageFetchers.Append(fetcherName).ToArray();
                        changed = true;
                    }
                    if (!typeOptions.ImageFetcherOrder.Contains(fetcherName, StringComparer.OrdinalIgnoreCase))
                    {
                        typeOptions.ImageFetcherOrder = typeOptions.ImageFetcherOrder.Append(fetcherName).ToArray();
                        changed = true;
                    }

                    if (changed)
                    {
                        store.Save(library);
                        activated++;
                        logger.Info("BookMeta : récupérateur de couvertures activé pour la bibliothèque {0}", library.Name);
                    }
                    else
                    {
                        logger.Debug("BookMeta : récupérateur de couvertures déjà actif pour la bibliothèque {0}", library.Name);
                    }

                    // Traitée (activée par nous ou déjà présente) : l'utilisateur garde ensuite la main.
                    processedIds.Add(library.Id);
                    persistProcessedIds();
                }
                catch (Exception ex)
                {
                    logger.Warn("BookMeta : activation du récupérateur de couvertures impossible pour la bibliothèque {0} ({1})", library?.Name ?? "?", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warn("BookMeta : activation automatique du récupérateur de couvertures abandonnée ({0})", ex.Message);
        }
        return activated;
    }

    private static TypeOptions FindOrCreateBookOptions(LibraryOptions options, out bool created)
    {
        var list = (options.TypeOptions ?? Array.Empty<TypeOptions>()).ToList();
        var existing = list.FirstOrDefault(t => string.Equals(t.Type, BookType, StringComparison.OrdinalIgnoreCase));
        created = existing == null;
        if (existing == null)
        {
            existing = new TypeOptions { Type = BookType };
            list.Add(existing);
            options.TypeOptions = list.ToArray();
        }
        existing.ImageFetchers ??= Array.Empty<string>();
        existing.ImageFetcherOrder ??= Array.Empty<string>();
        return existing;
    }
}
