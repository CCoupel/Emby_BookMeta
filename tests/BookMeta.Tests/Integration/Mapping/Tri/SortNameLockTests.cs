using BookMeta.Tests.Support;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;

// Lot : Integration/Mapping/Tri — régression recette emby2 D1 : BaseItem.AfterMetadataRefresh → ResetSortName remet le tri « calculé depuis le nom »
// (« La psy » → « psy ») sauf si MetadataFields.SortName ∈ LockedFields. Le provider verrouille donc le champ et pose SortName
// sur l'item RÉEL (BaseItem.LibraryManager.GetItemById(ItemInfo.Id)). La fusion Emby elle-même reste vérifiée en recette emby2.
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

        [SpecFact]
        public void Le_tri_compose_est_pose_sur_l_item_reel_et_le_champ_est_verrouille()
        {
            var real = new BookEntity { Name = "Tentation" };

            SpecApi.GetMetadata(Tentation(), _log, id => id == 42 ? real : null, itemId: 42);

            Assert.Equal("Meyer, Stephenie - Tentation", real.SortName);
            Assert.Contains(MetadataFields.SortName, real.LockedFields);
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
            var real = new BookEntity { Name = "La psy" };

            SpecApi.GetMetadata(BookFolder.Touch(dir, "La psy.epub"), _log, _ => real, itemId: 7);

            Assert.StartsWith("McFadden, Freida - ", real.SortName);
            Assert.Contains(MetadataFields.SortName, real.LockedFields);
        }

        [SpecFact]
        public void Un_second_passage_ne_duplique_pas_le_verrou()
        {
            var real = new BookEntity { Name = "Tentation" };
            var path = Tentation();

            SpecApi.GetMetadata(path, _log, _ => real, itemId: 42);
            SpecApi.GetMetadata(path, _log, _ => real, itemId: 42);

            Assert.Single(real.LockedFields, f => f == MetadataFields.SortName);
        }

        [SpecFact]
        public void Les_verrous_existants_sont_conserves()
        {
            var real = new BookEntity { Name = "Tentation", LockedFields = new[] { MetadataFields.Name } };

            SpecApi.GetMetadata(Tentation(), _log, _ => real, itemId: 42);

            Assert.Contains(MetadataFields.Name, real.LockedFields);
            Assert.Contains(MetadataFields.SortName, real.LockedFields);
        }

        [SpecFact]
        public void Un_tri_deja_verrouille_est_reecrit_avec_la_valeur_de_l_opf()
        {
            var real = new BookEntity { Name = "Tentation", LockedFields = new[] { MetadataFields.SortName } };
            real.SortName = "saisi a la main";

            SpecApi.GetMetadata(Tentation(), _log, _ => real, itemId: 42);

            Assert.Equal("Meyer, Stephenie - Tentation", real.SortName);
        }

        [SpecFact]
        public void Item_introuvable_les_metadonnees_sont_quand_meme_renvoyees_sans_exception()
        {
            var r = SpecApi.GetMetadata(Tentation(), _log, _ => null, itemId: 42);

            Assert.True(r.HasMetadata);
            Assert.Equal("Meyer, Stephenie - Tentation", r.Item.SortName);
        }

        [SpecFact]
        public void La_recherche_de_l_item_qui_echoue_est_journalisee_sans_exception()
        {
            var r = SpecApi.GetMetadata(Tentation(), _log, _ => throw new InvalidOperationException("boom"), itemId: 42);

            Assert.True(r.HasMetadata);
            Assert.True(_log.HasWarning);
        }

        [SpecFact]
        public void Un_item_qui_n_est_pas_un_livre_n_est_pas_touche()
        {
            var other = new MediaBrowser.Controller.Entities.Audio.Audio { Name = "x" };

            SpecApi.GetMetadata(Tentation(), _log, _ => other, itemId: 42);

            Assert.DoesNotContain(MetadataFields.SortName, other.LockedFields ?? Array.Empty<MetadataFields>());
        }

        [SpecFact]
        public void Id_zero_la_recherche_n_est_pas_faite()
        {
            var called = false;

            var r = SpecApi.GetMetadata(Tentation(), _log, _ => { called = true; return null; }, itemId: 0);

            Assert.False(called);
            Assert.True(r.HasMetadata);
        }

        [SpecFact]
        public void Sans_tri_exploitable_aucun_verrou_n_est_pose()
        {
            var dir = _fs.Dir("Auteur Test", "Rien", 2020);
            var real = new BookEntity { Name = "Rien" };

            SpecApi.GetMetadata(BookFolder.Touch(dir, "Rien.epub"), _log, _ => real, itemId: 5);

            Assert.DoesNotContain(MetadataFields.SortName, real.LockedFields ?? Array.Empty<MetadataFields>());
        }
    }
}
