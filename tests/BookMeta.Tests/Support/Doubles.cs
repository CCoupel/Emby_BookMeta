using System.Reflection;
using MediaBrowser.Model.IO;

namespace BookMeta.Tests.Support
{
    /// <summary>Double générique d'interface (DispatchProxy) : seules les méthodes utiles sont implémentées.</summary>
    public class StubProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var r = Handler?.Invoke(targetMethod!, args ?? Array.Empty<object?>());
            if (r == null && targetMethod!.ReturnType.IsValueType && targetMethod.ReturnType != typeof(void))
            {
                return Activator.CreateInstance(targetMethod.ReturnType);
            }
            return r;
        }

        public static T Create<T>(Func<MethodInfo, object?[], object?> handler) where T : class
        {
            var p = Create<T, StubProxy>();
            ((StubProxy)(object)p).Handler = handler;
            return p;
        }
    }

    /// <summary>Journal des appels de log (Debug/Info/Warn/Error…) d'un ILogger / ILogManager Emby.</summary>
    public class RecordingLogger
    {
        public List<(string Level, string Message)> Entries { get; } = new();

        public IEnumerable<string> Warnings => Entries.Where(e => e.Level == "Warn").Select(e => e.Message);

        public bool HasWarning => Entries.Any(e => e.Level == "Warn");

        public bool HasError => Entries.Any(e => e.Level.StartsWith("Error", StringComparison.Ordinal) || e.Level == "Fatal");

        public MediaBrowser.Model.Logging.ILogger Logger { get; }

        public MediaBrowser.Model.Logging.ILogManager Manager { get; }

        public RecordingLogger()
        {
            Logger = StubProxy.Create<MediaBrowser.Model.Logging.ILogger>((m, a) =>
            {
                var msg = a.Length > 0 && a[0] is string s ? s : "";
                if (a.Length > 1 && a[^1] is object[] p)
                {
                    try { msg = string.Format(msg, p); } catch (FormatException) { }
                }
                Entries.Add((m.Name, msg));
                return null;
            });
            Manager = StubProxy.Create<MediaBrowser.Model.Logging.ILogManager>((m, a) =>
                m.Name == "GetLogger" ? Logger : null);
        }
    }

    /// <summary>IDirectoryService / IFileSystem Emby adossés au vrai système de fichiers (dossier temporaire du test).</summary>
    public static class RealFs
    {
        public static FileSystemMetadata Meta(string path)
        {
            var isDir = Directory.Exists(path);
            var isFile = File.Exists(path);
            var meta = new FileSystemMetadata
            {
                FullName = path,
                Name = Path.GetFileName(path),
                Exists = isDir || isFile,
                IsDirectory = isDir,
            };
            if (isFile)
            {
                var fi = new FileInfo(path);
                meta.Length = fi.Length;
                meta.LastWriteTimeUtc = fi.LastWriteTimeUtc;
                meta.CreationTimeUtc = fi.CreationTimeUtc;
            }
            return meta;
        }

        private static bool CaseInsensitiveEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        public static MediaBrowser.Controller.Providers.IDirectoryService DirectoryService()
        {
            return StubProxy.Create<MediaBrowser.Controller.Providers.IDirectoryService>((m, a) =>
            {
                switch (m.Name)
                {
                    case "GetFiles":
                        return Directory.Exists((string)a[0]!)
                            ? Directory.GetFiles((string)a[0]!).Select(Meta).ToList()
                            : new List<FileSystemMetadata>();
                    case "GetFilePaths":
                        return Directory.Exists((string)a[0]!)
                            ? Directory.GetFiles((string)a[0]!).ToList()
                            : new List<string>();
                    case "GetFileSystemEntries":
                        return Directory.Exists((string)a[0]!)
                            ? Directory.GetFileSystemEntries((string)a[0]!).Select(Meta).ToArray()
                            : Array.Empty<FileSystemMetadata>();
                    case "GetFile" when a.Length == 1:
                        return Meta((string)a[0]!);
                    case "GetFile" when a.Length == 3:
                    {
                        var dir = (string)a[0]!;
                        var name = (string)a[1]!;
                        var cs = (bool)a[2]!;
                        if (!Directory.Exists(dir)) return null;
                        var hit = Directory.GetFiles(dir).FirstOrDefault(f =>
                            cs ? Path.GetFileName(f) == name : CaseInsensitiveEquals(Path.GetFileName(f), name));
                        return hit == null ? null : Meta(hit);
                    }
                    default:
                        return null;
                }
            });
        }

        public static IFileSystem FileSystem()
        {
            return StubProxy.Create<IFileSystem>((m, a) =>
            {
                switch (m.Name)
                {
                    case "GetFileInfo": return Meta((string)a[0]!);
                    case "OpenRead": return File.OpenRead((string)a[0]!);
                    case "FileExists": return File.Exists((string)a[0]!);
                    case "DirectoryExists": return Directory.Exists((string)a[0]!);
                    case "GetLastWriteTimeUtc": return File.GetLastWriteTimeUtc((string)a[0]!);
                    case "GetDirectoryName": return Path.GetDirectoryName((string)a[0]!);
                    case "GetFiles":
                        return Directory.Exists((string)a[0]!)
                            ? Directory.GetFiles((string)a[0]!, "*",
                                a.Length > 1 && a[1] is true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                                .Select(Meta).ToList()
                            : new List<FileSystemMetadata>();
                    default: return null;
                }
            });
        }
    }
}
