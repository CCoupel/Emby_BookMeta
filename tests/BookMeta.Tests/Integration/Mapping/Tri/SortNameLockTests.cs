using BookMeta.Tests.Support;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;

// Lot : Integration/Mapping/Tri — régression recette emby2 D1 (cycle 4) : BaseItem.AfterMetadataRefresh → ResetSortName remet le tri
// « calculé depuis le nom » (« La psy » → « psy ») sauf si MetadataFields.SortName ∈ LockedFields. BookSortNameProvider
// (ICustomMetadataProvider<Book> + IForcedProvider) s'exécute sur l'instance rafraîchie : FetchAsync pose le verrou et le tri
// sur MetadataResult<Book>.Item. La fusion Emby elle-même reste vérifiée en recette emby2.
namespace BookMeta.Tests.Integration.Mapping.Tri
{
    [Trait("Lot", "Integration/Mapping/Tri")]
    public class SortNameLockTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private string Tentation()
        {
            var dir = _fs.Dir("Stephenie Meyer", "Tentation", 2006);
            BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");
            return BookFolder.Touch(dir, "Tentation.epub");
        }

        private static bool IsLocked(BaseItem item) => (item.LockedFields ?? Array.Empty<MetadataFields>()).Contains(MetadataFields.SortName);

        [SpecFact]
        public void Le_tri_compose_est_pose_sur_l_instance_rafraichie_et_le_champ_est_verrouille()
        {
            var item = SpecApi.NewBookItem(Tentation());

            var update = SpecApi.FetchSortName(item, _log);

            Assert.Equal("Meyer, Stephenie - Tentation", item.SortName);
            Assert.True(IsLocked(item));
            Assert.Equal(ItemUpdateType.MetadataEdit, update);
        }

        [SpecFact]
        public void Le_tri_d_un_titre_a_article_n_est_pas_celui_calcule_par_Emby()
        {
            var dir = _fs.Dir("Freida McFadden", "La psy", 2020);
            BookFolder.WriteSidecarText(dir,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><package xmlns=\"http://www.idpf.org/2007/opf\" version=\"2.0\">" +
                "<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:opf=\"http://www.idpf.org/2007/opf\">" +
                "<dc:title>La psy</dc:title><meta name=\"calibre:title_sort\" content=\"psy (La)\"/>" +
                "<dc:creator opf:role=\"aut\" opf:file-as=\"McFadden, Freida\">Freida McFadden</dc:creator></metadata></package>");
            var item = SpecApi.NewBookItem(BookFolder.Touch(dir, "La psy.epub"));
            item.Name = "La psy";

            SpecApi.FetchSortName(item, _log);

            Assert.StartsWith("McFadden, Freida - ", item.SortName);
            Assert.True(IsLocked(item));
        }

        [SpecFact]
        public void Le_tri_de_serie_est_pose()
        {
            var dir = _fs.Dir("Stephenie Meyer", "Tentation", 2006);
            BookFolder.PlaceSidecar(dir, "series.opf");
            var item = SpecApi.NewBookItem(BookFolder.Touch(dir, "Tentation.epub"));

            SpecApi.FetchSortName(item, _log);

            Assert.Equal("Meyer, Stephenie - Twilight 02 - Tentation", item.SortName);
        }

        [SpecFact]
        public void Un_second_passage_ne_duplique_pas_le_verrou_et_ne_signale_aucun_changement()
        {
            var item = SpecApi.NewBookItem(Tentation());

            SpecApi.FetchSortName(item, _log);
            var second = SpecApi.FetchSortName(item, _log);

            Assert.Single(item.LockedFields, f => f == MetadataFields.SortName);
            Assert.Equal(ItemUpdateType.None, second);
            Assert.Equal("Meyer, Stephenie - Tentation", item.SortName);
        }

        [SpecFact]
        public void Les_verrous_existants_sont_conserves()
        {
            var item = SpecApi.NewBookItem(Tentation());
            item.LockedFields = new[] { MetadataFields.Name };

            SpecApi.FetchSortName(item, _log);

            Assert.Contains(MetadataFields.Name, item.LockedFields);
            Assert.True(IsLocked(item));
        }

        [SpecFact]
        public void Un_tri_deja_verrouille_mais_different_est_reecrit_avec_la_valeur_de_l_opf()
        {
            var item = SpecApi.NewBookItem(Tentation());
            item.LockedFields = new[] { MetadataFields.SortName };
            item.SortName = "saisi a la main";

            var update = SpecApi.FetchSortName(item, _log);

            Assert.Equal("Meyer, Stephenie - Tentation", item.SortName);
            Assert.Equal(ItemUpdateType.MetadataEdit, update);
        }

        [SpecFact]
        public void Sans_OPF_ni_epub_lisible_rien_n_est_modifie()
        {
            var dir = _fs.Dir("Auteur Test", "Rien", 2020);
            var item = SpecApi.NewBookItem(BookFolder.Touch(dir, "Rien.pdf"));

            var update = SpecApi.FetchSortName(item, _log);

            Assert.Equal(ItemUpdateType.None, update);
            Assert.False(IsLocked(item));
        }

        [SpecFact]
        public void Sans_titre_donc_sans_tri_exploitable_aucun_verrou_n_est_pose()
        {
            var dir = _fs.Dir("Auteur Test", "Sans titre", 2020);
            BookFolder.PlaceSidecar(dir, "no-title.opf");
            var item = SpecApi.NewBookItem(BookFolder.Touch(dir, "Sans titre.epub"));

            SpecApi.FetchSortName(item, _log);

            Assert.False(IsLocked(item));
        }

        [SpecFact]
        public void Item_sans_Path_rien_n_est_modifie()
        {
            var item = new BookEntity { Name = "factice" };

            Assert.Equal(ItemUpdateType.None, SpecApi.FetchSortName(item, _log));
            Assert.False(IsLocked(item));
        }

        [SpecFact]
        public void Resultat_sans_item_aucune_exception()
        {
            Assert.Equal(ItemUpdateType.None, SpecApi.FetchSortName(null, _log));
        }

        [SpecFact]
        public void OPF_corrompu_et_epub_corrompu_aucune_exception_ni_verrou()
        {
            var dir = _fs.Dir("Auteur Test", "Corrompu", 2020);
            BookFolder.PlaceSidecar(dir, "invalid.opf");
            var item = SpecApi.NewBookItem(BookFolder.Touch(dir, "Corrompu.epub", new byte[] { 1, 2, 3 }));

            var update = SpecApi.FetchSortName(item, _log);

            Assert.Equal(ItemUpdateType.None, update);
            Assert.False(IsLocked(item));
        }
    }
}
