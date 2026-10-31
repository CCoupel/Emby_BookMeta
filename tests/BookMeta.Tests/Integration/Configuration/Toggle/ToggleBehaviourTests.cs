using BookMeta.Tests.Support;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;

// Lot : Integration/Configuration/Toggle — #8 : l'option EnableEpubCoverExtraction pilote le récupérateur « BookMeta Embedded Cover » :
// epub → désactivable ; cbz et couverture de dossier → jamais concernés.
namespace BookMeta.Tests.Integration.Configuration.Toggle
{
    [Trait("Lot", "Integration/Configuration/Toggle")]
    public class ToggleBehaviourTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private string Epub(byte[] cover)
        {
            var epub = Path.Combine(_fs.Dir("Auteur Test", "Livre", 2020), "Livre.epub");
            Archives.WriteEpub(epub, Archives.Opf2("T", "c", ("c", "c.jpg", "image/jpeg")), files: new[] { ("OEBPS/c.jpg", cover) });
            return epub;
        }

        [SpecFact]
        public void Option_activee_Supports_epub_vrai_et_GetImage_renvoie_la_couverture()
        {
            var cover = Img.Jpg("epub");
            var epub = Epub(cover);

            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = epub }, _log, epubEnabled: true));
            Assert.Equal(cover, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(epub, _log, epubEnabled: true)!));
        }

        [SpecFact]
        public void Option_desactivee_Supports_epub_faux_et_GetImage_repond_vide()
        {
            var epub = Epub(Img.Jpg("epub"));

            Assert.False(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = epub }, _log, epubEnabled: false));
            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log, epubEnabled: false)));
        }

        [SpecFact]
        public void Option_desactivee_le_cbz_est_inchange()
        {
            var cbz = Path.Combine(_fs.Dir("Darko Macan", "Frankenwood", 2026), "Frankenwood.cbz");
            var first = Img.Png("p1");
            Archives.WriteZip(cbz, ("001.png", first));

            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = cbz }, _log, epubEnabled: false));
            Assert.Equal(first, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(cbz, _log, epubEnabled: false)!));
        }

        [SpecFact]
        public void Option_desactivee_la_couverture_du_dossier_est_inchangee()
        {
            var epub = Epub(Img.Jpg("epub"));
            BookFolder.Touch(Path.GetDirectoryName(epub)!, "cover.jpg", Img.Jpg("dossier"));

            // Le fournisseur de dossier ne lit pas l'option : il continue de proposer cover.jpg.
            Assert.Single(SpecApi.GetFolderCovers(epub, _log));
            Assert.True(SpecApi.SupportsFolderCovers(new BookEntity { Path = epub }, _log));
        }

        [SpecFact]
        public void Option_desactivee_le_livre_factice_reste_propose_dans_les_reglages_de_bibliotheque()
        {
            // Sinon le récupérateur disparaîtrait de la liste (régression D2) alors que l'utilisateur a seulement désactivé l'epub.
            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity(), _log, epubEnabled: false));
            Assert.True(SpecApi.SupportsEmbeddedCover(new BookEntity { Path = "/config/metadata/dummy" }, _log, epubEnabled: false));
        }

        [SpecFact]
        public void Reactiver_l_option_redonne_la_couverture_epub_sans_etat_residuel()
        {
            var cover = Img.Jpg("epub");
            var epub = Epub(cover);

            Assert.False(SpecApi.HasImage(SpecApi.GetEmbeddedCover(epub, _log, epubEnabled: false)));
            Assert.Equal(cover, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(epub, _log, epubEnabled: true)!));
        }

        [SpecFact]
        public void Sans_configuration_chargee_le_defaut_est_actif()
        {
            // Aucun override (Plugin.Instance absent en test) → comportement par défaut : extraction activée.
            var cover = Img.Jpg("epub");
            var epub = Epub(cover);

            Assert.Equal(cover, SpecApi.ReadImage(SpecApi.GetEmbeddedCover(epub, _log)!));
        }
    }
}
