using MediaBrowser.Model.Entities;
using BookMeta.Tests.Support;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;

// Lot : Integration/Covers/Folder — #7 : cover.* / folder.* du dossier du livre (contrat §4 priorité 1), ILocalImageFileProvider.
namespace BookMeta.Tests.Integration.Covers.Folder
{
    [Trait("Lot", "Integration/Covers/Folder")]
    public class FolderCoverTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private (string Book, string Dir) NewBook(string author = "Auteur Test", string title = "Livre")
        {
            var dir = _fs.Dir(author, title, 2020);
            return (BookFolder.Touch(dir, title + ".epub"), dir);
        }

        private List<string> CoverNames(string book) =>
            SpecApi.GetFolderCovers(book, _log).Select(i => i.FileInfo.Name).ToList();

        [SpecTheory]
        [InlineData("cover.jpg")]
        [InlineData("cover.jpeg")]
        [InlineData("cover.png")]
        [InlineData("cover.webp")]
        [InlineData("folder.jpg")]
        [InlineData("folder.jpeg")]
        [InlineData("folder.png")]
        [InlineData("folder.webp")]
        public void Les_noms_cover_et_folder_sont_reconnus(string fileName)
        {
            var (book, dir) = NewBook();
            BookFolder.Touch(dir, fileName, Img.Jpg("x"));

            var covers = SpecApi.GetFolderCovers(book, _log);

            Assert.Single(covers);
            Assert.Equal(fileName, covers[0].FileInfo.Name);
            Assert.Equal(ImageType.Primary, covers[0].Type);
        }

        [SpecTheory]
        [InlineData("COVER.JPG")]
        [InlineData("Cover.Png")]
        [InlineData("FOLDER.jpg")]
        public void La_casse_est_ignoree(string fileName)
        {
            var (book, dir) = NewBook();
            BookFolder.Touch(dir, fileName, Img.Jpg("x"));

            Assert.Single(CoverNames(book));
        }

        [SpecFact]
        public void Cover_prime_sur_folder()
        {
            var (book, dir) = NewBook();
            BookFolder.Touch(dir, "folder.jpg", Img.Jpg("f"));
            BookFolder.Touch(dir, "cover.png", Img.Png("c"));

            var names = CoverNames(book);

            Assert.NotEmpty(names);
            Assert.Equal("cover.png", names[0]);
        }

        [SpecFact]
        public void Aucune_image_dans_le_dossier_donne_une_liste_vide()
        {
            var (book, _) = NewBook();

            Assert.Empty(SpecApi.GetFolderCovers(book, _log));
        }

        [SpecTheory]
        [InlineData("cover-back.jpg")]
        [InlineData("mycover.jpg")]
        [InlineData("cover.txt")]
        [InlineData("cover.gif")]
        [InlineData("couverture.jpg")]
        public void Les_autres_noms_ou_extensions_sont_ignores(string fileName)
        {
            var (book, dir) = NewBook();
            BookFolder.Touch(dir, fileName, Img.Jpg("x"));

            Assert.Empty(SpecApi.GetFolderCovers(book, _log));
        }

        [SpecFact]
        public void Le_livre_et_le_sidecar_ne_sont_jamais_pris_pour_une_couverture()
        {
            var (book, dir) = NewBook();
            BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");

            Assert.Empty(SpecApi.GetFolderCovers(book, _log));
        }

        [SpecFact]
        public void Dossier_avec_apostrophe_et_accents()
        {
            var (book, dir) = NewBook("Stéphanie D'Été", "L'appel du sang");
            BookFolder.Touch(dir, "cover.jpg", Img.Jpg("x"));

            Assert.Single(SpecApi.GetFolderCovers(book, _log));
        }

        [SpecFact]
        public void Seule_l_image_Primary_est_fournie_et_le_fichier_est_celui_du_dossier()
        {
            var (book, dir) = NewBook();
            BookFolder.Touch(dir, "cover.jpg", Img.Jpg("x"));

            var info = SpecApi.GetFolderCovers(book, _log).Single();

            Assert.Equal(Path.Combine(dir, "cover.jpg"), info.FileInfo.FullName);
            Assert.All(SpecApi.GetFolderCovers(book, _log), i => Assert.Equal(ImageType.Primary, i.Type));
        }

        [SpecFact]
        public void Le_fournisseur_supporte_les_livres_seulement_et_garde_son_nom()
        {
            Assert.True(SpecApi.SupportsFolderCovers(new BookEntity(), _log));
            Assert.False(SpecApi.SupportsFolderCovers(new MediaBrowser.Controller.Entities.Audio.Audio(), _log));
            Assert.Equal("BookMeta Cover File", SpecApi.FolderCoverProviderName(_log));
        }
    }
}
