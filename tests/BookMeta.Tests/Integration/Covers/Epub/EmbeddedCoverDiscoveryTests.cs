using BookMeta.Tests.Support;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;

// Lot : Integration/Covers/Epub — régression recette emby2 D2 : Emby liste les fetchers d'images avec un item FACTICE sans Path.
// Si Supports renvoie false pour cet item, le fetcher n'est jamais proposé ni appelé (ImageFetchers = [] constaté).
namespace BookMeta.Tests.Integration.Covers.Epub
{
    [Trait("Lot", "Integration/Covers/Epub")]
    public class EmbeddedCoverDiscoveryTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        [SpecFact]
        public void Embedded_Supports_vrai_pour_un_Book_sans_Path()
        {
            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity(), _log));
        }

        [SpecFact]
        public void Embedded_Supports_vrai_pour_un_Book_au_Path_vide()
        {
            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = "" }, _log));
        }

        [SpecTheory]
        [InlineData("/config/metadata/dummy")]
        [InlineData("dummy")]
        [InlineData("/var/lib/emby/metadata/dummy")]
        public void D2_bis_Embedded_Supports_vrai_pour_le_livre_factice_dont_le_Path_est_dummy_sans_extension(string path)
        {
            // Emby (ProviderManager.GetPluginSummary) liste les fetchers avec un Book dont le chemin se termine par « dummy ».
            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = path }, _log));
        }

        [SpecTheory]
        [InlineData("/config/metadata/dummy")]
        [InlineData("dummy")]
        public void D2_bis_GetImage_sur_le_livre_factice_dummy_repond_vide(string path)
        {
            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(path, _log)));
        }

        [SpecFact]
        public void Embedded_Supports_faux_pour_un_item_qui_n_est_pas_un_Book()
        {
            Assert.False(SpecApi.SupportsEmbeddedCover(new MediaBrowser.Controller.Entities.Audio.Audio(), _log));
        }

        [SpecFact]
        public void Folder_Supports_vrai_pour_un_Book_sans_Path()
        {
            Assert.True(SpecApi.SupportsFolderCovers(new BookEntity(), _log));
        }

        [SpecTheory]
        [InlineData("/ceph/books/A/L (2020)/L.epub", true, true)]
        [InlineData("/ceph/books/A/L (2020)/L.epub", false, false)]
        [InlineData("/ceph/books/A/L (2020)/L.EPUB", false, false)]
        [InlineData("/ceph/books/A/L (2020)/L.cbz", true, true)]
        [InlineData("/ceph/books/A/L (2020)/L.cbz", false, true)]
        public void L_option_epub_est_respectee_par_Supports(string path, bool epubEnabled, bool expected)
        {
            Assert.Equal(expected, SpecApi.SupportsEmbeddedCover(new BookEntity { Path = path }, _log, epubEnabled));
        }

        [SpecFact]
        public void Option_epub_desactivee_GetImage_epub_est_vide_sans_exception()
        {
            var dir = _fs.Dir("Auteur Test", "Livre", 2020);
            var epub = System.IO.Path.Combine(dir, "Livre.epub");
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "c.jpg", "image/jpeg")),
                files: new[] { ("OEBPS/c.jpg", Img.Jpg("x")) });

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log, epubEnabled: false)));
        }

        [SpecFact]
        public void Option_epub_desactivee_le_cbz_garde_sa_couverture()
        {
            var dir = _fs.Dir("Auteur Test", "BD", 2020);
            var cbz = System.IO.Path.Combine(dir, "BD.cbz");
            var first = Img.Jpg("p1");
            Archives.WriteZip(cbz, ("001.jpg", first));

            var r = SpecApi.GetEmbeddedCover(cbz, _log, epubEnabled: false);

            Assert.Equal(first, SpecApi.ReadImage(r!));
        }

        [SpecFact]
        public void GetImage_sur_un_Book_sans_Path_repond_vide_sans_exception()
        {
            var r = SpecApi.GetEmbeddedCoverPathless(_log);

            Assert.False(SpecApi.HasImage(r));
        }

        [SpecTheory]
        [InlineData("Livre.pdf")]
        [InlineData("Livre.cbr")]
        [InlineData("Livre.txt")]
        [InlineData("Livre")]
        public void GetImage_sur_une_extension_non_geree_repond_vide_sans_exception(string fileName)
        {
            var dir = _fs.Dir("Auteur Test", "Livre", 2020);
            var path = BookFolder.Touch(dir, fileName, Img.Jpg("x"));

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(path, _log)));
        }

        [SpecFact]
        public void GetImage_sur_un_fichier_absent_repond_vide_sans_exception()
        {
            var absent = System.IO.Path.Combine(_fs.Root, "absent (2020)", "absent.epub");

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(absent, _log)));
        }
    }
}
