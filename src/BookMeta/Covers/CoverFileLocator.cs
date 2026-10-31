using MediaBrowser.Model.IO;

namespace BookMeta.Covers;

/// <summary><c>cover.*</c> puis <c>folder.*</c> (jpg, jpeg, png, webp ; casse ignorée) dans le dossier du livre.</summary>
public static class CoverFileLocator
{
    private static readonly string[] BaseNames = { "cover", "folder" };
    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public static FileSystemMetadata? Find(IEnumerable<FileSystemMetadata> files)
    {
        var list = files.Where(f => f != null && !f.IsDirectory && !string.IsNullOrEmpty(f.Name)).ToList();
        foreach (var baseName in BaseNames)
            foreach (var ext in Extensions)
            {
                var hit = list.FirstOrDefault(f => string.Equals(f.Name, baseName + ext, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
        return null;
    }
}
