using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

// Lot : Integration/Plugin/Smoke (tag smoke) — #1, #4.
// NB : le namespace contient un segment « Plugin » ; le type testé est donc toujours qualifié global::BookMeta.Plugin.
namespace BookMeta.Tests.Integration.Plugin.Smoke
{
    [Trait("Tag", "smoke")]
    [Trait("Lot", "Integration/Plugin/Smoke")]
    public class PluginSmokeTests
    {
        // GUID fixé au squelette (#1), noté dans contracts/models.md §6. Il ne doit JAMAIS changer :
        // il identifie la configuration persistée et la page de configuration.
        private const string ExpectedPluginId = "097ed1ba-f0ae-496f-8f56-e54599fe54b1";

        // Version du milestone (X.Y.Z) ; le 4e champ « a » est incrémenté par deployer au /publish : non testé.
        private const string ExpectedMilestone = "0.1.0";

        private static Type PluginType => typeof(global::BookMeta.Plugin);

        private static object NewUninitializedPlugin() => RuntimeHelpers.GetUninitializedObject(PluginType);

        [Fact]
        public void Assembly_est_nommee_BookMeta()
        {
            Assert.Equal("BookMeta", PluginType.Assembly.GetName().Name);
        }

        [Fact]
        public void Le_type_Plugin_derive_de_BasePlugin()
        {
            var baseType = PluginType.BaseType;
            Assert.NotNull(baseType);
            Assert.True(baseType!.IsGenericType, "Plugin doit dériver de BasePlugin<TConfig>");
            Assert.Equal("BasePlugin`1", baseType.GetGenericTypeDefinition().Name);
            Assert.Equal("MediaBrowser.Common.Plugins", baseType.Namespace);
        }

        [Fact]
        public void Le_type_Plugin_est_public_et_concret()
        {
            Assert.True(PluginType.IsPublic);
            Assert.False(PluginType.IsAbstract);
        }

        [Fact]
        public void Plugin_Id_est_un_guid_non_vide()
        {
            var id = (Guid)PluginType.GetProperty("Id")!.GetValue(NewUninitializedPlugin())!;
            Assert.NotEqual(Guid.Empty, id);
        }

        [Fact]
        public void Plugin_Id_est_le_guid_fige_au_squelette()
        {
            var id = (Guid)PluginType.GetProperty("Id")!.GetValue(NewUninitializedPlugin())!;
            Assert.Equal(Guid.Parse(ExpectedPluginId), id);
        }

        [Fact]
        public void Plugin_Name_est_BookMeta()
        {
            var name = (string)PluginType.GetProperty("Name")!.GetValue(NewUninitializedPlugin())!;
            Assert.Equal("BookMeta", name);
        }

        [Fact]
        public void Version_assembly_egale_celle_du_csproj()
        {
            var doc = XDocument.Load(RepoPaths.PluginCsproj);
            var csprojVersion = doc.Descendants("Version").Select(e => e.Value.Trim()).FirstOrDefault();
            Assert.False(string.IsNullOrEmpty(csprojVersion), "<Version> absente de BookMeta.csproj");

            Assert.Equal(Version.Parse(csprojVersion!), PluginType.Assembly.GetName().Version);
        }

        [Fact]
        public void Version_assembly_est_celle_du_milestone()
        {
            // À mettre à jour à chaque nouveau milestone (X.Y.Z) ; le 4e champ est ignoré.
            var v = PluginType.Assembly.GetName().Version!;
            Assert.Equal(ExpectedMilestone, v.Major + "." + v.Minor + "." + v.Build);
        }

        [Fact]
        public void Le_csproj_cible_net6_et_ne_copie_pas_les_dll_du_SDK_Emby()
        {
            var doc = XDocument.Load(RepoPaths.PluginCsproj);
            Assert.Equal("net6.0", doc.Descendants("TargetFramework").Single().Value.Trim());

            var refs = doc.Descendants("Reference")
                .Where(r => ((string?)r.Attribute("Include") ?? "").StartsWith("MediaBrowser.", StringComparison.Ordinal))
                .ToList();
            Assert.NotEmpty(refs);
            foreach (var r in refs)
            {
                Assert.Equal("false", r.Element("Private")?.Value.Trim().ToLowerInvariant());
            }
        }
    }
}
