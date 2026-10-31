using BookMeta.Tests.Support;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;
using ImageFormat = MediaBrowser.Model.Drawing.ImageFormat;

// Lot : Integration/Covers/Cbz — #7 : première image de l'archive .cbz par tri naturel (contrat §4 priorité 3).
namespace BookMeta.Tests.Integration.Covers.Cbz
{
    [Trait("Lot", "Integration/Covers/Cbz")]
    public class CbzCoverTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private string CbzPath(string title = "Frankenwood") =>
            Path.Combine(_fs.Dir("Darko Macan", title, 2026), title + ".cbz");

        [SpecFact]
        public void La_premiere_image_en_ordre_naturel_est_la_couverture()
        {
            var cbz = CbzPath();
            var first = Img.Jpg("page-2");
            // Ordre alphabétique pur donnerait page10 avant page2 : le tri doit être naturel.
            Archives.WriteZip(cbz, ("page10.jpg", Img.Jpg("page-10")), ("page2.jpg", first), ("page11.jpg", Img.Jpg("page-11")));

            var r = SpecApi.GetEmbeddedCover(cbz, _log);

            Assert.True(SpecApi.HasImage(r));
            Assert.Equal(first, SpecApi.ReadImage(r!));
        }

        [SpecFact]
        public void Entrees_dans_un_sous_dossier_et_ordre_de_stockage_quelconque()
        {
            var cbz = CbzPath();
            var first = Img.Png("p1");
            Archives.WriteZip(cbz, ("Livre/003.png", Img.Png("p3")), ("Livre/001.png", first), ("Livre/002.png", Img.Png("p2")));

            Assert.Equal(first, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(cbz, _log)!));
        }

        [SpecFact]
        public void Le_dossier_MACOSX_est_exclu()
        {
            var cbz = CbzPath();
            var real = Img.Jpg("vraie");
            Archives.WriteZip(cbz, ("__MACOSX/._001.jpg", Img.Jpg("parasite")), ("__MACOSX/001.jpg", Img.Jpg("parasite2")), ("002.jpg", real));

            Assert.Equal(real, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(cbz, _log)!));
        }

        [SpecFact]
        public void Les_fichiers_caches_sont_exclus()
        {
            var cbz = CbzPath();
            var real = Img.Jpg("vraie");
            Archives.WriteZip(cbz, (".hidden.jpg", Img.Jpg("cache")), ("._001.jpg", Img.Jpg("appledouble")), ("010.jpg", real));

            Assert.Equal(real, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(cbz, _log)!));
        }

        [SpecFact]
        public void Les_fichiers_non_image_sont_ignores()
        {
            var cbz = CbzPath();
            var real = Img.Jpg("vraie");
            Archives.WriteZip(cbz, ("000-ComicInfo.xml", new byte[] { 1 }), ("Thumbs.db", new byte[] { 2 }), ("001.jpg", real));

            Assert.Equal(real, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(cbz, _log)!));
        }

        [SpecTheory]
        [InlineData("001.jpg", ImageFormat.Jpg)]
        [InlineData("001.jpeg", ImageFormat.Jpg)]
        [InlineData("001.png", ImageFormat.Png)]
        [InlineData("001.webp", ImageFormat.Webp)]
        [InlineData("001.gif", ImageFormat.Gif)]
        [InlineData("001.JPG", ImageFormat.Jpg)]
        public void Le_format_est_deduit_de_l_extension(string entry, ImageFormat expected)
        {
            var cbz = CbzPath();
            Archives.WriteZip(cbz, (entry, Img.Jpg("x")));

            Assert.Equal(expected, SpecApi.GetEmbeddedCover(cbz, _log)!.Format);
        }

        [SpecFact]
        public void Archive_sans_image_pas_de_couverture()
        {
            var cbz = CbzPath();
            Archives.WriteZip(cbz, ("ComicInfo.xml", new byte[] { 1 }));

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(cbz, _log)));
        }

        [SpecFact]
        public void Archive_vide_pas_de_couverture()
        {
            var cbz = CbzPath();
            Archives.WriteZip(cbz);

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(cbz, _log)));
        }

        [SpecFact]
        public void Archive_corrompue_pas_de_couverture_Warn_et_pas_d_exception()
        {
            var cbz = CbzPath();
            File.WriteAllText(cbz, "pas un zip");

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(cbz, _log)));
            Assert.True(_log.HasWarning);
        }

        [SpecFact]
        public void Image_vide_n_est_pas_une_couverture()
        {
            var cbz = CbzPath();
            Archives.WriteZip(cbz, ("001.jpg", Array.Empty<byte>()));

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(cbz, _log)));
        }

        [SpecFact]
        public void Cbz_le_chemin_est_pris_sur_BaseItem_quand_MetadataResult_Item_n_en_a_pas()
        {
            // Revue LOT-2 M2 (voir EpubCoverTests).
            var cbz = CbzPath();
            var first = Img.Jpg("via-baseitem");
            Archives.WriteZip(cbz, ("001.jpg", first), ("002.jpg", Img.Jpg("p2")));

            var r = SpecApi.GetEmbeddedCoverFromBaseItemOnly(cbz, _log);

            Assert.True(SpecApi.HasImage(r));
            Assert.Equal(first, SpecApi.ReadImage(r!));
        }

        [SpecFact]
        public void Supports_cbz_oui()
        {
            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = "/ceph/books/D/F (2026)/F.cbz" }, _log));
            // Recette D2 : cbr / pdf ne sont plus filtrés par Supports (item factice sans Path) mais par GetImage (réponse vide).
        }

        [SpecFact]
        public void Le_cbr_n_a_pas_de_couverture_embarquee()
        {
            var dir = _fs.Dir("Darko Macan", "Frankenwood", 2026);
            var cbr = BookFolder.Touch(dir, "Frankenwood.cbr", Img.Jpg("x"));

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(cbr, _log)));
        }

        [SpecFact]
        public void Le_pdf_du_meme_dossier_n_a_pas_de_couverture_embarquee()
        {
            var dir = _fs.Dir("Darko Macan", "Frankenwood", 2026);
            var pdf = BookFolder.Touch(dir, "Frankenwood.pdf");
            Archives.WriteZip(Path.Combine(dir, "Frankenwood.cbz"), ("001.jpg", Img.Jpg("x")));

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(pdf, _log)));
        }
    }
}
