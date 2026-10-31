using System.IO.Compression;
using System.Text;

namespace BookMeta.Tests.Support
{
    /// <summary>
    /// Dossier temporaire reproduisant l'arborescence /ceph/books/&lt;Auteur&gt;/&lt;Titre&gt; (&lt;Année&gt;)/ ;
    /// supprimé au Dispose. Les epub/cbz sont générés à la volée (aucun binaire committé).
    /// </summary>
    public sealed class BookFolder : IDisposable
    {
        public string Root { get; } = Path.Combine(AppContext.BaseDirectory, "tmp", Guid.NewGuid().ToString("N"));

        public BookFolder() => Directory.CreateDirectory(Root);

        /// <summary>Crée &lt;Root&gt;/&lt;auteur&gt;/&lt;titre&gt; (&lt;année&gt;)/ et renvoie son chemin.</summary>
        public string Dir(string author = "Auteur Test", string title = "Livre", int year = 2020)
        {
            var d = Path.Combine(Root, author, title + " (" + year + ")");
            Directory.CreateDirectory(d);
            return d;
        }

        public static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", "opf", name);

        public static string FixtureText(string name) => File.ReadAllText(FixturePath(name), Encoding.UTF8);

        /// <summary>Copie une fixture OPF comme metadata.opf du dossier.</summary>
        public static void PlaceSidecar(string dir, string fixtureName) =>
            File.Copy(FixturePath(fixtureName), Path.Combine(dir, "metadata.opf"), overwrite: true);

        public static void WriteSidecarText(string dir, string content) =>
            File.WriteAllText(Path.Combine(dir, "metadata.opf"), content, new UTF8Encoding(false));

        public static string Touch(string dir, string fileName, byte[]? content = null)
        {
            var p = Path.Combine(dir, fileName);
            File.WriteAllBytes(p, content ?? new byte[] { 1 });
            return p;
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>Octets d'images factices mais reconnaissables (signature + marqueur) : seule l'identité des octets compte.</summary>
    public static class Img
    {
        private static byte[] Make(byte[] sig, string marker) => sig.Concat(Encoding.ASCII.GetBytes(marker)).ToArray();

        public static byte[] Png(string marker) => Make(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, marker);
        public static byte[] Jpg(string marker) => Make(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, marker);
        public static byte[] Gif(string marker) => Make(Encoding.ASCII.GetBytes("GIF89a"), marker);
        public static byte[] Webp(string marker) => Make(Encoding.ASCII.GetBytes("RIFF....WEBP"), marker);
    }

    public static class Archives
    {
        public static void WriteZip(string path, params (string Name, byte[] Data)[] entries)
        {
            using var fs = File.Create(path);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
            foreach (var (name, data) in entries)
            {
                var e = zip.CreateEntry(name);
                using var s = e.Open();
                s.Write(data, 0, data.Length);
            }
        }

        public static (string, byte[]) Text(string name, string content) => (name, new UTF8Encoding(false).GetBytes(content));

        public const string ContainerXml =
            "<?xml version=\"1.0\"?><container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">" +
            "<rootfiles><rootfile full-path=\"{0}\" media-type=\"application/oebps-package+xml\"/></rootfiles></container>";

        /// <summary>
        /// Écrit un epub : mimetype, META-INF/container.xml (rootfile = opfPath), l'OPF, puis les fichiers fournis.
        /// withContainer=false → archive sans container.xml (OPF interne illisible).
        /// </summary>
        public static void WriteEpub(string path, string opfXml, string opfPath = "OEBPS/content.opf",
            bool withContainer = true, params (string Name, byte[] Data)[] files)
        {
            var list = new List<(string, byte[])> { Text("mimetype", "application/epub+zip") };
            if (withContainer)
            {
                list.Add(Text("META-INF/container.xml", string.Format(ContainerXml, opfPath)));
            }
            list.Add(Text(opfPath, opfXml));
            list.AddRange(files);
            WriteZip(path, list.ToArray());
        }

        /// <summary>OPF 2 minimal d'un epub : titre, éventuelle meta cover, éléments de manifest (id, href, media-type).</summary>
        public static string Opf2(string title, string? coverId, params (string Id, string Href, string MediaType)[] manifest)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?><package xmlns=\"http://www.idpf.org/2007/opf\" version=\"2.0\" unique-identifier=\"uid\">");
            sb.Append("<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:opf=\"http://www.idpf.org/2007/opf\">");
            sb.Append("<dc:title>").Append(title).Append("</dc:title><dc:identifier id=\"uid\">x</dc:identifier>");
            if (coverId != null) sb.Append("<meta name=\"cover\" content=\"").Append(coverId).Append("\"/>");
            sb.Append("</metadata><manifest>");
            foreach (var (id, href, mt) in manifest)
                sb.Append("<item id=\"").Append(id).Append("\" href=\"").Append(href).Append("\" media-type=\"").Append(mt).Append("\"/>");
            sb.Append("</manifest><spine/></package>");
            return sb.ToString();
        }

        /// <summary>OPF 3 minimal : item(s) de manifest avec propriétés éventuelles (ex. "cover-image").</summary>
        public static string Opf3(string title, params (string Id, string Href, string MediaType, string? Properties)[] manifest)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?><package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"uid\">");
            sb.Append("<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:title>").Append(title)
              .Append("</dc:title><dc:identifier id=\"uid\">x</dc:identifier></metadata><manifest>");
            foreach (var (id, href, mt, props) in manifest)
            {
                sb.Append("<item id=\"").Append(id).Append("\" href=\"").Append(href).Append("\" media-type=\"").Append(mt).Append('"');
                if (props != null) sb.Append(" properties=\"").Append(props).Append('"');
                sb.Append("/>");
            }
            sb.Append("</manifest><spine/></package>");
            return sb.ToString();
        }
    }
}
