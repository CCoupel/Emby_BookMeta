using System;
using System.IO;

namespace BookMeta.Tests.Integration
{
    /// <summary>Localise la racine du dépôt depuis le dossier d'exécution des tests.</summary>
    internal static class RepoPaths
    {
        public static string Root { get; } = FindRoot();

        public static string Libs => Path.Combine(Root, "libs");

        public static string PluginCsproj => Path.Combine(Root, "src", "BookMeta", "BookMeta.csproj");

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "BookMeta.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Racine du dépôt introuvable (BookMeta.sln) depuis " + AppContext.BaseDirectory);
        }
    }
}
