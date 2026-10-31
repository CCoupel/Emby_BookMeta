using MediaBrowser.Model.Drawing;

namespace BookMeta.Covers;

public static class ImageFormats
{
    public static ImageFormat? FromExtension(string? extension) =>
        (extension ?? string.Empty).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpg,
            ".png" => ImageFormat.Png,
            ".webp" => ImageFormat.Webp,
            ".gif" => ImageFormat.Gif,
            _ => null,
        };

    public static ImageFormat? FromMediaType(string? mediaType) =>
        (mediaType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" or "image/pjpeg" => ImageFormat.Jpg,
            "image/png" => ImageFormat.Png,
            "image/webp" => ImageFormat.Webp,
            "image/gif" => ImageFormat.Gif,
            _ => null,
        };
}

/// <summary>Image extraite d'une archive (copie en mémoire, position 0).</summary>
public sealed record CoverImage(Stream Data, ImageFormat Format);
