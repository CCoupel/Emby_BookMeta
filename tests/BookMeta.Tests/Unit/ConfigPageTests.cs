using System.Reflection;
using MediaBrowser.Model.Plugins;
using Xunit;

namespace BookMeta.Tests.Unit;

public class ConfigPageTests
{
    private static string Resource(string name)
    {
        var asm = typeof(Plugin).Assembly;
        using var s = asm.GetManifestResourceStream("BookMeta.Configuration." + name);
        Assert.NotNull(s);
        return new StreamReader(s!).ReadToEnd();
    }

    [Fact]
    public void Ressources_SontEmbarquees()
    {
        Assert.Contains("EnableEpubCoverExtraction", Resource("configPage.html"));
        Assert.Contains("BookMetaConfigScript", Resource("configPage.html"));
        Assert.False(string.IsNullOrWhiteSpace(Resource("configScript.js")));
    }

    [Fact]
    public void Page_ExposeLaCase_ActiveeParDefaut_SansLesIdentifiantsDeBibliotheques()
    {
        var html = Resource("configPage.html");
        Assert.Matches("id=\"EnableEpubCoverExtraction\"[^>]*\\bchecked\\b", html);
        Assert.Contains("BookMeta Embedded Cover", html);
        Assert.DoesNotContain("AutoEnabledLibraryIds", html);
    }

    [Fact]
    public void Script_UtiliseLeGuidDuPlugin_NeTouchePasAutoEnabledLibraryIds()
    {
        var js = Resource("configScript.js");
        Assert.Contains(Plugin.PluginId.ToString(), js);
        Assert.Contains("getPluginConfiguration", js);
        Assert.Contains("updatePluginConfiguration", js);
        Assert.DoesNotContain("AutoEnabledLibraryIds", js);
        // une seule propriété de la configuration est affectée
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(js, @"cfg\.\w+\s*="));
    }

    [Fact]
    public void GetPages_DeclarePageEtScript_AvecEntreeDeMenu()
    {
        var plugin = (Plugin)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Plugin));
        var pages = plugin.GetPages().ToList();
        var page = pages.Single(p => p.Name == "BookMetaConfig");
        Assert.True(page.EnableInMainMenu);
        Assert.Equal("BookMeta", page.DisplayName);
        Assert.Contains(pages, p => p.Name == "BookMetaConfigScript");
        foreach (var p in pages)
            Assert.NotNull(typeof(Plugin).Assembly.GetManifestResourceStream(p.EmbeddedResourcePath));
    }
}
