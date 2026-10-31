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
