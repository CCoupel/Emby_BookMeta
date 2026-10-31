using BookMeta.Tests.Support;
using Xunit;
using BookEntity = MediaBrowser.Controller.Entities.Book;

// Lot : Integration/Mapping/Book — #6 : correspondance OPF → Book (contracts/models.md §2 ; décisions GATE 2 : auteur et série en Tags, SortName composé).
namespace BookMeta.Tests.Integration.Mapping.Book
{
    [Trait("Lot", "Integration/Mapping/Book")]
    public class BookMappingTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private MediaBrowser.Controller.Providers.MetadataResult<BookEntity> MapFixture(string fixture)
        {
            var dir = _fs.Dir("Auteur Test", "Livre " + fixture.Replace(".opf", ""), 2020);
            BookFolder.PlaceSidecar(dir, fixture);
            return SpecApi.GetMetadata(BookFolder.Touch(dir, "livre.epub"), _log);
        }

        private MediaBrowser.Controller.Providers.MetadataResult<BookEntity> MapXml(string opfXml)
        {
            var dir = _fs.Dir("Auteur Test", "Livre inline " + Guid.NewGuid().ToString("N")[..6], 2020);
            BookFolder.WriteSidecarText(dir, opfXml);
            return SpecApi.GetMetadata(BookFolder.Touch(dir, "livre.epub"), _log);
        }

        private static string Opf2(string metadataInner) =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><package xmlns=\"http://www.idpf.org/2007/opf\" version=\"2.0\">" +
            "<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:opf=\"http://www.idpf.org/2007/opf\">" +
            metadataInner + "</metadata></package>";

        // ---------- Auteurs ----------

        [SpecFact]
        public void Un_tag_par_auteur_role_aut_ou_sans_role_les_autres_roles_sont_exclus()
        {
            var r = MapFixture("full.opf");

            Assert.Contains("Alice Martin", r.Item.Tags);
            Assert.Contains("Bob Durand", r.Item.Tags);
            Assert.DoesNotContain("Claire Petit", r.Item.Tags);
        }

        [SpecFact]
        public void Aucune_personne_n_est_renseignee_SupportsPeople_est_faux_dans_Emby()
        {
            var r = MapFixture("full.opf");

            Assert.Empty(r.People ?? new List<MediaBrowser.Controller.Entities.PersonInfo>());
        }

        [SpecFact]
        public void Auteur_sans_file_as_le_tri_derive_Nom_Prenom_du_dernier_mot()
        {
            var r = MapXml(Opf2("<dc:title>Titre</dc:title><dc:creator opf:role=\"aut\">Stephenie Meyer</dc:creator>"));

            Assert.Equal("Meyer, Stephenie - Titre", r.Item.SortName);
        }

        // ---------- SortName ----------

        [SpecFact]
        public void SortName_avec_auteur_et_title_sort()
        {
            var r = MapFixture("tentation-bindery.opf");

            Assert.Equal("Meyer, Stephenie - Tentation", r.Item.SortName);
        }

        [SpecFact]
        public void SortName_sans_auteur_vaut_title_sort()
        {
            var r = MapXml(Opf2("<dc:title>Le Titre</dc:title><meta name=\"calibre:title_sort\" content=\"Titre, Le\"/>"));

            Assert.Equal("Titre, Le", r.Item.SortName);
        }

        [SpecFact]
        public void SortName_sans_title_sort_ni_auteur_vaut_le_nom()
        {
            var r = MapFixture("minimal.opf");

            Assert.Equal("Seulement un titre", r.Item.SortName);
        }

        [SpecFact]
        public void SortName_sans_title_sort_utilise_le_nom_apres_l_auteur()
        {
            var r = MapXml(Opf2("<dc:title>Mon Titre</dc:title><dc:creator opf:role=\"aut\" opf:file-as=\"Dupont, Jean\">Jean Dupont</dc:creator>"));

            Assert.Equal("Dupont, Jean - Mon Titre", r.Item.SortName);
        }

        // ---------- Régression recette emby2 D1 : le tri doit être porté par l'item après mapping ----------
        // Constat : SortName = titre seul sur les 17 items. Le tri composé doit survivre à la fusion Emby :
        // « Tri effectif » = SpecApi.EffectiveSortName : ForcedSortName si l'item en porte un (membre non public du SDK 4.10.2,
        // lu par réflexion), sinon SortName. À aligner sur le mécanisme retenu par dev-plugin (SDK_NOTES).
        // Limite : la fusion MetadataService n'est pas reproductible hors serveur → vérification finale = recette emby2.

        [SpecFact]
        public void D1_Tentation_le_tri_effectif_porte_le_tri_compose()
        {
            var r = MapFixture("tentation-bindery.opf");

            Assert.Equal("Meyer, Stephenie - Tentation", SpecApi.EffectiveSortName(r.Item));
            Assert.Equal("Meyer, Stephenie - Tentation", r.Item.SortName);
        }

        [SpecFact]
        public void D1_Serie_le_tri_effectif_porte_auteur_serie_index_titre()
        {
            var r = MapFixture("series.opf");

            Assert.Equal("Meyer, Stephenie - Twilight 02 - Tentation", SpecApi.EffectiveSortName(r.Item));
        }

        [SpecFact]
        public void D1_Titre_a_article_le_tri_compose_n_est_pas_le_titre_seul()
        {
            // « La psy » / « psy » : le tri Emby par défaut retire l'article ; le tri BookMeta commence par l'auteur.
            var r = MapXml(Opf2("<dc:title>La psy</dc:title><meta name=\"calibre:title_sort\" content=\"psy (La)\"/>" +
                "<dc:creator opf:role=\"aut\" opf:file-as=\"McFadden, Freida\">Freida McFadden</dc:creator>"));

            Assert.StartsWith("McFadden, Freida - ", SpecApi.EffectiveSortName(r.Item));
            Assert.StartsWith("McFadden, Freida - ", r.Item.SortName);
        }

        [SpecFact]
        public void D1_Sans_auteur_le_tri_effectif_vaut_le_titre_de_tri()
        {
            var r = MapXml(Opf2("<dc:title>Le Titre</dc:title><meta name=\"calibre:title_sort\" content=\"Titre, Le\"/>"));

            Assert.Equal("Titre, Le", SpecApi.EffectiveSortName(r.Item));
        }

        // ---------- Série (GATE 2 Q2) ----------

        [SpecFact]
        public void Serie_calibre_donne_un_tag_Serie()
        {
            var r = MapFixture("series.opf");

            Assert.Contains("Série : Twilight", r.Item.Tags);
        }

        [SpecFact]
        public void Serie_calibre_compose_le_SortName_auteur_serie_index_titre()
        {
            var r = MapFixture("series.opf");

            Assert.Equal("Meyer, Stephenie - Twilight 02 - Tentation", r.Item.SortName);
        }

        [SpecFact]
        public void Serie_index_a_deux_chiffres_n_est_pas_tronque()
        {
            var r = MapXml(Opf2("<dc:title>T</dc:title><meta name=\"calibre:title_sort\" content=\"T\"/>" +
                "<dc:creator opf:role=\"aut\" opf:file-as=\"Auteur, Un\">Un Auteur</dc:creator>" +
                "<meta name=\"calibre:series\" content=\"Saga\"/><meta name=\"calibre:series_index\" content=\"10\"/>"));

            Assert.Equal("Auteur, Un - Saga 10 - T", r.Item.SortName);
        }

        [SpecFact]
        public void Serie_ne_renseigne_pas_IndexNumber()
        {
            var r = MapFixture("series.opf");

            Assert.Null(r.Item.IndexNumber);
        }

        [SpecFact]
        public void Sans_serie_aucun_tag_Serie()
        {
            var r = MapFixture("tentation-bindery.opf");

            Assert.DoesNotContain(r.Item.Tags, t => t.StartsWith("Série :", StringComparison.Ordinal));
        }

        // ---------- Résumé ----------

        [SpecFact]
        public void Resume_entites_html_decodees_et_balises_retirees()
        {
            var r = MapFixture("entities.opf");

            Assert.NotNull(r.Item.Overview);
            Assert.Contains("'bonjour'", r.Item.Overview);
            Assert.Contains("& sourit", r.Item.Overview);
            Assert.DoesNotContain("<", r.Item.Overview);
            Assert.DoesNotContain("&#39;", r.Item.Overview);
            Assert.DoesNotContain("&amp;", r.Item.Overview);
        }

        [SpecFact]
        public void Resume_nbsp_devient_un_espace_et_les_blancs_sont_normalises()
        {
            var r = MapFixture("entities.opf");

            Assert.Contains("Elle dit : 'bonjour'", r.Item.Overview.Replace(' ', ' '));
            Assert.DoesNotContain("  ", r.Item.Overview);
        }

        [SpecFact]
        public void Resume_paragraphes_et_br_deviennent_des_sauts_de_ligne()
        {
            var r = MapFixture("entities.opf");

            Assert.Contains("\n", r.Item.Overview);
            Assert.Contains("Fin\nLigne suivante", r.Item.Overview.Replace("\r\n", "\n"));
        }

        [SpecFact]
        public void Titre_avec_entites_est_decode()
        {
            var r = MapFixture("entities.opf");

            Assert.Equal("L'été & la nuit", r.Item.Name);
        }

        // ---------- Dates ----------

        [SpecTheory]
        [InlineData("2006-01-01", 2006, 1, 1)]
        [InlineData("2019-05", 2019, 5, 1)]
        [InlineData("1999", 1999, 1, 1)]
        [InlineData("2021-03-15T10:30:00Z", 2021, 3, 15)]
        [InlineData("2021-03-15T10:30:00+00:00", 2021, 3, 15)]
        public void Date_formats_acceptes(string dcDate, int year, int month, int day)
        {
            var r = MapXml(Opf2("<dc:title>T</dc:title><dc:date>" + dcDate + "</dc:date>"));

            Assert.Equal(year, r.Item.ProductionYear);
            var d = r.Item.PremiereDate!.Value.UtcDateTime;
            Assert.Equal((year, month, day), (d.Year, d.Month, d.Day));
        }

        [SpecTheory]
        [InlineData("pas une date")]
        [InlineData("")]
        [InlineData("2021-13-45")]
        public void Date_invalide_est_ignoree_le_reste_est_conserve(string dcDate)
        {
            var r = MapXml(Opf2("<dc:title>Garde le titre</dc:title><dc:date>" + dcDate + "</dc:date>"));

            Assert.True(r.HasMetadata);
            Assert.Equal("Garde le titre", r.Item.Name);
            Assert.Null(r.Item.PremiereDate);
            Assert.Null(r.Item.ProductionYear);
        }

        [SpecTheory]
        [InlineData("<dc:date opf:event=\"creation\">2000-01-01</dc:date><dc:date opf:event=\"modification\">2023-05-05</dc:date><dc:date opf:event=\"publication\">2006-03-04</dc:date>")]
        [InlineData("<dc:date opf:event=\"modification\">2023-05-05</dc:date><dc:date opf:event=\"publication\">2006-03-04</dc:date>")]
        [InlineData("<dc:date opf:event=\"modification\">2023-05-05</dc:date><dc:date>2006-03-04</dc:date><dc:date opf:event=\"creation\">2000-01-01</dc:date>")]
        public void Plusieurs_dates_OPF2_la_publication_est_retenue(string datesXml)
        {
            // Revue LOT-2 : opf:event creation / modification ne doivent pas fausser l'année ni PremiereDate.
            var r = MapXml(Opf2("<dc:title>T</dc:title>" + datesXml));

            Assert.Equal(2006, r.Item.ProductionYear);
            Assert.Equal(new DateTime(2006, 3, 4), r.Item.PremiereDate!.Value.UtcDateTime.Date);
        }

        [SpecFact]
        public void Date_avec_offset_conserve_l_annee_d_origine()
        {
            // Revue LOT-2 : 2020-12-31T23:30:00-05:00 vaut 2021-01-01Z en UTC mais l'année de l'OPF est 2020.
            var r = MapXml(Opf2("<dc:title>T</dc:title><dc:date>2020-12-31T23:30:00-05:00</dc:date>"));

            Assert.Equal(2020, r.Item.ProductionYear);
        }

        [SpecTheory]
        [InlineData("-3")]
        [InlineData("1e300")]
        [InlineData("99999999999")]
        [InlineData("0.001")]
        [InlineData("-0.5")]
        public void Index_de_serie_negatif_ou_demesure_est_ignore_dans_le_tri(string index)
        {
            // Revue LOT-2 : pas de « -3 » ni d'int.MinValue dans le SortName ; le tag de série et le reste du tri restent.
            var r = MapXml(Opf2("<dc:title>Titre</dc:title><meta name=\"calibre:title_sort\" content=\"Titre\"/>" +
                "<dc:creator opf:role=\"aut\" opf:file-as=\"Auteur, Un\">Un Auteur</dc:creator>" +
                "<meta name=\"calibre:series\" content=\"Saga\"/><meta name=\"calibre:series_index\" content=\"" + index + "\"/>"));

            Assert.Contains("Série : Saga", r.Item.Tags);
            Assert.StartsWith("Auteur, Un", r.Item.SortName);
            Assert.EndsWith("Titre", r.Item.SortName);
            Assert.DoesNotContain("-3", r.Item.SortName);
            Assert.DoesNotContain("-2147483648", r.Item.SortName);
            Assert.DoesNotContain("2147483647", r.Item.SortName);
            Assert.DoesNotContain("E+", r.Item.SortName);
            Assert.DoesNotContain("99999999999", r.Item.SortName);
            Assert.DoesNotContain("  ", r.Item.SortName);
        }

        // ---------- Éditeur, sujets ----------

        [SpecFact]
        public void Un_studio_par_publisher()
        {
            var r = MapFixture("full.opf");

            Assert.Equal(new[] { "Hachette", "Le Livre de Poche" }, r.Item.Studios.OrderBy(s => s).ToArray());
        }

        [SpecFact]
        public void Un_genre_par_sujet_dedoublonne_sans_tenir_compte_de_la_casse()
        {
            var r = MapFixture("full.opf");

            Assert.Equal(2, r.Item.Genres.Length);
            Assert.Contains(r.Item.Genres, g => string.Equals(g, "Fantastique", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("Jeunesse", r.Item.Genres);
        }

        // ---------- Identifiants ----------

        [SpecFact]
        public void Identifiants_BNF_BINDERY_ISBN_sont_des_ProviderIds()
        {
            var r = MapFixture("full.opf");

            Assert.Equal("cb12345678x", r.Item.ProviderIds["BNF"]);
            Assert.Equal("12", r.Item.ProviderIds["BINDERY"]);
            Assert.Equal("9782070368228", r.Item.ProviderIds["ISBN"]);
        }

        [SpecFact]
        public void Les_autres_identifiants_sont_ignores()
        {
            var r = MapFixture("full.opf");

            Assert.False(r.Item.ProviderIds.ContainsKey("UUID"));
            Assert.Equal(3, r.Item.ProviderIds.Count);
        }

        [SpecTheory]
        [InlineData("<dc:identifier opf:scheme=\"ISBN\">978-2-07-036822-8</dc:identifier>", "9782070368228")]
        [InlineData("<dc:identifier>urn:isbn:9782070368228</dc:identifier>", "9782070368228")]
        [InlineData("<dc:identifier>urn:isbn:978-2-07-036822-8</dc:identifier>", "9782070368228")]
        public void ISBN_est_nettoye_des_tirets_et_du_prefixe_urn(string identifierXml, string expected)
        {
            var r = MapXml(Opf2("<dc:title>T</dc:title>" + identifierXml));

            Assert.Equal(expected, r.Item.ProviderIds["ISBN"]);
        }

        // ---------- OPF 3 ----------

        [SpecFact]
        public void OPF3_titre_et_tri_via_refines_file_as()
        {
            var r = MapFixture("opf3-sample.opf");

            Assert.Equal("Le Nom du Vent", r.Item.Name);
            Assert.Contains("Nom du Vent, Le", r.Item.SortName);
        }

        [SpecFact]
        public void OPF3_auteur_role_aut_en_tag_illustrateur_exclu()
        {
            var r = MapFixture("opf3-sample.opf");

            Assert.Contains("Patrick Rothfuss", r.Item.Tags);
            Assert.DoesNotContain("Jane Doe", r.Item.Tags);
        }

        [SpecFact]
        public void OPF3_serie_belongs_to_collection_et_group_position()
        {
            var r = MapFixture("opf3-sample.opf");

            Assert.Contains("Série : Chronique du tueur de roi", r.Item.Tags);
            Assert.Equal("Rothfuss, Patrick - Chronique du tueur de roi 02 - Nom du Vent, Le", r.Item.SortName);
        }

        [SpecFact]
        public void OPF3_editeur_sujets_isbn_date()
        {
            var r = MapFixture("opf3-sample.opf");

            Assert.Contains("Gallimard", r.Item.Studios);
            Assert.Equal(2, r.Item.Genres.Length);
            Assert.Equal("9782070368228", r.Item.ProviderIds["ISBN"]);
            Assert.Equal(2019, r.Item.ProductionYear);
        }

        // ---------- Divers ----------

        [SpecFact]
        public void Attributs_sans_prefixe_opf_sont_acceptes()
        {
            var r = MapXml(Opf2("<dc:title>T</dc:title><dc:creator role=\"aut\" file-as=\"Durand, Bob\">Bob Durand</dc:creator>" +
                "<dc:identifier scheme=\"BNF\">cb999</dc:identifier>"));

            Assert.Contains("Bob Durand", r.Item.Tags);
            Assert.Equal("cb999", r.Item.ProviderIds["BNF"]);
        }

        [SpecFact]
        public void La_langue_n_est_pas_un_champ_du_livre_mais_alimente_ResultLanguage()
        {
            var r = MapFixture("full.opf");

            Assert.Equal("fr", r.ResultLanguage);
        }

        [SpecFact]
        public void Les_espaces_autour_du_titre_sont_retires()
        {
            var r = MapXml(Opf2("<dc:title>   Titre espacé \n </dc:title>"));

            Assert.Equal("Titre espacé", r.Item.Name);
        }
    }
}
