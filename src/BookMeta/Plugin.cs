using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace BookMeta;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>GUID du plugin — ne jamais le modifier apres le premier deploiement.</summary>
    public static readonly Guid PluginId = new Guid("097ed1ba-f0ae-496f-8f56-e54599fe54b1");

    public static Plugin? Instance { get; private set; }

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override Guid Id => PluginId;

    public override string Name => "BookMeta";

    public override string Description => "Renseigne la bibliothèque Livres depuis les fichiers locaux (metadata.opf, epub, cbz), sans appel réseau.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name                 = "BookMetaConfig",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
                EnableInMainMenu     = true,
                DisplayName          = "BookMeta",
                MenuSection          = "server",
                MenuIcon             = "menu_book"
            },
            new PluginPageInfo
            {
                Name                 = "BookMetaConfigScript",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configScript.js"
            }
        };
    }
}
