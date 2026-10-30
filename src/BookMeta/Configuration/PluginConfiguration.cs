using MediaBrowser.Model.Plugins;

namespace BookMeta;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Extraire la couverture depuis l'epub quand aucun cover.* / folder.* n'est présent (défaut : oui).</summary>
    public bool EnableEpubCoverExtraction { get; set; } = true;
}
