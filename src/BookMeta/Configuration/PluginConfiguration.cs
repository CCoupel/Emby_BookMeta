using MediaBrowser.Model.Plugins;

namespace BookMeta;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Extraire la couverture depuis l'epub quand aucun cover.* / folder.* n'est présent (défaut : oui).</summary>
    public bool EnableEpubCoverExtraction { get; set; } = true;

    /// <summary>
    /// Identifiants des bibliothèques de livres pour lesquelles « BookMeta Embedded Cover » a déjà été activé automatiquement
    /// (une seule fois par bibliothèque : une case décochée ensuite par l'utilisateur n'est jamais recochée).
    /// </summary>
    public List<string> AutoEnabledLibraryIds { get; set; } = new();
}
