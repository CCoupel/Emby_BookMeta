using BookMeta.Tests.Support;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;
using ImageFormat = MediaBrowser.Model.Drawing.ImageFormat;

// Lot : Integration/Covers/Epub — #7 : couverture extraite de l'epub (contrat §4 priorité 2), IDynamicImageProvider.
namespace BookMeta.Tests.Integration.Covers.Epub
{
    [Trait("Lot", "Integration/Covers/Epub")]
    public class EpubCoverTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private string EpubPath(string title = "Livre") =>
            Path.Combine(_fs.Dir("Auteur Test", title, 2020), title + ".epub");

        [SpecFact]
        public void OPF2_meta_cover_designe_l_item_du_manifest()
        {
            var epub = EpubPath();
            var cover = Img.Jpg("couverture-opf2");
            Archives.WriteEpub(epub, Archives.Opf2("T", "cov",
                ("a", "Images/a.png", "image/png"), ("cov", "Images/cover.jpg", "image/jpeg")),
                files: new[] { ("OEBPS/Images/a.png", Img.Png("autre")), ("OEBPS/Images/cover.jpg", cover) });

            var r = SpecApi.GetEmbeddedCover(epub, _log);

            Assert.True(SpecApi.HasImage(r));
            Assert.Equal(cover, SpecApi.ReadImage(r!));
            Assert.Equal(ImageFormat.Jpg, r!.Format);
        }

        [SpecFact]
        public void OPF3_properties_cover_image()
        {
            var epub = EpubPath();
            var cover = Img.Png("couverture-opf3");
            Archives.WriteEpub(epub, Archives.Opf3("T",
                ("a", "a.jpg", "image/jpeg", null), ("c", "c.png", "image/png", "cover-image")),
                files: new[] { ("OEBPS/a.jpg", Img.Jpg("autre")), ("OEBPS/c.png", cover) });

            var r = SpecApi.GetEmbeddedCover(epub, _log);

            Assert.Equal(cover, SpecApi.ReadImage(r!));
            Assert.Equal(ImageFormat.Png, r!.Format);
        }

        [SpecFact]
        public void Sans_declaration_de_couverture_la_premiere_image_du_manifest()
        {
            var epub = EpubPath();
            var first = Img.Jpg("premiere");
            Archives.WriteEpub(epub, Archives.Opf2("T", null,
                ("css", "style.css", "text/css"), ("p1", "p1.jpg", "image/jpeg"), ("p2", "p2.png", "image/png")),
                files: new[] { ("OEBPS/style.css", new byte[] { 1 }), ("OEBPS/p1.jpg", first), ("OEBPS/p2.png", Img.Png("seconde")) });

            Assert.Equal(first, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(epub, _log)!));
        }

        [SpecFact]
        public void Meta_cover_prime_sur_la_premiere_image()
        {
            var epub = EpubPath();
            var cover = Img.Png("la-vraie");
            Archives.WriteEpub(epub, Archives.Opf2("T", "c",
                ("p1", "p1.jpg", "image/jpeg"), ("c", "c.png", "image/png")),
                files: new[] { ("OEBPS/p1.jpg", Img.Jpg("pas-la-bonne")), ("OEBPS/c.png", cover) });

            Assert.Equal(cover, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(epub, _log)!));
        }

        [SpecFact]
        public void Epub_sans_aucune_image_pas_de_couverture_et_pas_d_exception()
        {
            var epub = EpubPath();
            Archives.WriteEpub(epub, Archives.Opf2("T", null, ("t", "t.xhtml", "application/xhtml+xml")),
                files: new[] { ("OEBPS/t.xhtml", new byte[] { 1 }) });

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log)));
        }

        [SpecFact]
        public void Href_avec_pourcentages_et_sous_dossiers_relatifs_a_l_opf()
        {
            var epub = EpubPath();
            var cover = Img.Jpg("href-encode");
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "Mes%20Images/cover%20page.jpg", "image/jpeg")),
                opfPath: "OEBPS/Text/content.opf",
                files: new[] { ("OEBPS/Text/Mes Images/cover page.jpg", cover) });

            Assert.Equal(cover, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(epub, _log)!));
        }

        [SpecFact]
        public void Href_remontant_avec_double_point()
        {
            var epub = EpubPath();
            var cover = Img.Png("remonte");
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "../Images/cover.png", "image/png")),
                opfPath: "OEBPS/Text/content.opf",
                files: new[] { ("OEBPS/Images/cover.png", cover) });

            Assert.Equal(cover, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(epub, _log)!));
        }

        [SpecFact]
        public void Couverture_declaree_mais_entree_absente_de_l_archive_pas_d_exception()
        {
            var epub = EpubPath();
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "manquante.jpg", "image/jpeg")));

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log)));
            Assert.True(_log.HasWarning);
        }

        [SpecFact]
        public void Image_vide_n_est_pas_une_couverture()
        {
            var epub = EpubPath();
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "c.jpg", "image/jpeg")),
                files: new[] { ("OEBPS/c.jpg", Array.Empty<byte>()) });

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log)));
        }

        [SpecFact]
        public void Type_d_image_inconnu_pas_de_couverture()
        {
            var epub = EpubPath();
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "c.tiff", "image/tiff")),
                files: new[] { ("OEBPS/c.tiff", new byte[] { 1, 2, 3 }) });

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log)));
        }

        [SpecFact]
        public void Epub_corrompu_pas_de_couverture_Warn_et_pas_d_exception()
        {
            var epub = EpubPath();
            File.WriteAllText(epub, "pas un zip");

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log)));
            Assert.True(_log.HasWarning);
        }

        [SpecFact]
        public void Epub_sans_container_pas_de_couverture()
        {
            var epub = EpubPath();
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "c.jpg", "image/jpeg")), withContainer: false,
                files: new[] { ("OEBPS/c.jpg", Img.Jpg("x")) });

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log)));
        }

        [SpecFact]
        public void Aucune_ecriture_sur_disque_le_dossier_du_livre_reste_inchange()
        {
            var epub = EpubPath();
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "c.jpg", "image/jpeg")),
                files: new[] { ("OEBPS/c.jpg", Img.Jpg("x")) });
            var dir = Path.GetDirectoryName(epub)!;
            var before = Directory.GetFileSystemEntries(dir, "*", SearchOption.AllDirectories).OrderBy(x => x).ToArray();

            SpecApi.GetEmbeddedCover(epub, _log);

            Assert.Equal(before, Directory.GetFileSystemEntries(dir, "*", SearchOption.AllDirectories).OrderBy(x => x).ToArray());
        }

        [SpecFact]
        public void Supports_epub_oui_pdf_non_et_nom_stable()
        {
            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = "/ceph/books/A/L (2020)/L.epub" }, _log));
            Assert.False(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = "/ceph/books/A/L (2020)/L.pdf" }, _log));
            Assert.Equal("BookMeta Embedded Cover", SpecApi.EmbeddedCoverProviderName(_log));
        }

        [SpecFact]
        public void Seule_l_image_Primary_est_annoncee()
        {
            var epub = EpubPath();

            Assert.Equal(new[] { MediaBrowser.Model.Entities.ImageType.Primary }, SpecApi.EmbeddedSupportedImages(epub, _log));
        }
    }
}
