using System;
using System.IO;
using System.Reflection;
using Xunit;

// Lot : Integration/Sdk/Version — #1, #3 : les DLL du SDK committées dans libs/ sont celles d'Emby 4.10.2.0.
// (Les libs d'Emby_Badges sont en 4.9.3.0 : ne doivent pas être copiées.)
namespace BookMeta.Tests.Integration.Sdk.Version
{
    public class SdkVersionTests
    {
        private static readonly System.Version Attendue = new System.Version(4, 10, 2, 0);

        [Theory]
        [InlineData("MediaBrowser.Common.dll")]
        [InlineData("MediaBrowser.Controller.dll")]
        [InlineData("MediaBrowser.Model.dll")]
        public void La_lib_du_SDK_existe_en_version_4_10_2_0(string dll)
        {
            var path = Path.Combine(RepoPaths.Libs, dll);
            Assert.True(File.Exists(path), "libs/" + dll + " absente");
            Assert.Equal(Attendue, AssemblyName.GetAssemblyName(path).Version);
        }

        [Fact]
        public void Aucune_assembly_interne_Emby_nest_committee_dans_libs()
        {
            // Emby.* (Providers, Server.Implementations, …) : lecture seule, jamais référencées ni committées.
            var internes = Directory.GetFiles(RepoPaths.Libs, "Emby.*.dll");
            Assert.Empty(internes);
        }

        [Fact]
        public void Les_libs_du_SDK_sont_suivies_par_git()
        {
            // libs/ est committé volontairement (CI) : le .gitignore ne doit pas l'exclure.
            var gitignore = Path.Combine(RepoPaths.Root, ".gitignore");
            if (!File.Exists(gitignore)) return;

            // Dernière règle applicable gagne : si *.dll / libs sont ignorés, une réinclusion (!libs/*.dll) doit suivre.
            var ignore = false;
            foreach (var line in File.ReadAllLines(gitignore))
            {
                var l = line.Trim();
                if (l == "libs/" || l == "libs" || l == "/libs/" || l == "*.dll") ignore = true;
                else if (l == "!libs/" || l == "!libs/*.dll" || l == "!/libs/*.dll") ignore = false;
            }
            Assert.False(ignore, "libs/*.dll est gitignoré sans réinclusion (!libs/*.dll)");
        }
    }
}
