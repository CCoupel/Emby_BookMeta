using System.Xml.Serialization;
using Xunit;

// Lot : Integration/Configuration/Toggle — #8 : EnableEpubCoverExtraction (défaut true) et persistance de PluginConfiguration
// (XML /config/plugins/configurations/BookMeta.xml, sérialiseur XmlSerializer comme IXmlSerializer d'Emby).
// AutoEnabledLibraryIds (état interne : bibliothèques où le récupérateur a déjà été activé automatiquement) ne doit JAMAIS être perdu
// par un enregistrement de la page, qui ne modifie que la case.
namespace BookMeta.Tests.Integration.Configuration.Toggle
{
    [Trait("Lot", "Integration/Configuration/Toggle")]
    public class PluginConfigurationTests
    {
        private static readonly XmlSerializer Serializer = new(typeof(global::BookMeta.PluginConfiguration));

        private static string ToXml(global::BookMeta.PluginConfiguration c)
        {
            using var w = new StringWriter();
            Serializer.Serialize(w, c);
            return w.ToString();
        }

        private static global::BookMeta.PluginConfiguration FromXml(string xml)
        {
            using var r = new StringReader(xml);
            return (global::BookMeta.PluginConfiguration)Serializer.Deserialize(r)!;
        }

        [Fact]
        public void Par_defaut_l_extraction_epub_est_activee()
        {
            Assert.True(new global::BookMeta.PluginConfiguration().EnableEpubCoverExtraction);
        }

        [Fact]
        public void Par_defaut_aucune_bibliotheque_n_est_activee_automatiquement_et_la_liste_n_est_pas_nulle()
        {
            var c = new global::BookMeta.PluginConfiguration();

            Assert.NotNull(c.AutoEnabledLibraryIds);
            Assert.Empty(c.AutoEnabledLibraryIds);
        }

        [Fact]
        public void Aller_retour_XML_conserve_l_option_et_les_bibliotheques_activees()
        {
            var c = new global::BookMeta.PluginConfiguration
            {
                EnableEpubCoverExtraction = false,
                AutoEnabledLibraryIds = new List<string> { "lib-1", "lib-2" },
            };

            var back = FromXml(ToXml(c));

            Assert.False(back.EnableEpubCoverExtraction);
            Assert.Equal(new[] { "lib-1", "lib-2" }, back.AutoEnabledLibraryIds);
        }

        [Fact]
        public void Decocher_puis_enregistrer_ne_touche_que_la_case()
        {
            // Simule la page : charger la configuration, ne modifier que la case, ré-enregistrer.
            var loaded = FromXml(ToXml(new global::BookMeta.PluginConfiguration { AutoEnabledLibraryIds = new List<string> { "lib-1" } }));
            loaded.EnableEpubCoverExtraction = false;

            var saved = FromXml(ToXml(loaded));

            Assert.False(saved.EnableEpubCoverExtraction);
            Assert.Equal(new[] { "lib-1" }, saved.AutoEnabledLibraryIds);
        }

        [Fact]
        public void Recocher_apres_decochage_conserve_aussi_les_bibliotheques()
        {
            var c = FromXml(ToXml(new global::BookMeta.PluginConfiguration { EnableEpubCoverExtraction = false, AutoEnabledLibraryIds = new List<string> { "a" } }));
            c.EnableEpubCoverExtraction = true;

            var saved = FromXml(ToXml(c));

            Assert.True(saved.EnableEpubCoverExtraction);
            Assert.Equal(new[] { "a" }, saved.AutoEnabledLibraryIds);
        }

        [Fact]
        public void Un_ancien_fichier_sans_AutoEnabledLibraryIds_se_charge_avec_les_valeurs_par_defaut()
        {
            // BookMeta.xml écrit par une version antérieure (0.1.0.x) : seule l'option existe.
            const string old = "<?xml version=\"1.0\" encoding=\"utf-8\"?><PluginConfiguration xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><EnableEpubCoverExtraction>false</EnableEpubCoverExtraction></PluginConfiguration>";

            var c = FromXml(old);

            Assert.False(c.EnableEpubCoverExtraction);
            Assert.NotNull(c.AutoEnabledLibraryIds);
            Assert.Empty(c.AutoEnabledLibraryIds);
        }

        [Fact]
        public void Un_fichier_vide_de_contenu_garde_l_option_activee()
        {
            const string empty = "<?xml version=\"1.0\" encoding=\"utf-8\"?><PluginConfiguration xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" />";

            Assert.True(FromXml(empty).EnableEpubCoverExtraction);
        }

        [Fact]
        public void Un_element_inconnu_d_une_version_future_est_ignore()
        {
            const string future = "<?xml version=\"1.0\" encoding=\"utf-8\"?><PluginConfiguration><EnableEpubCoverExtraction>false</EnableEpubCoverExtraction>" +
                "<OptionDuFutur>x</OptionDuFutur></PluginConfiguration>";

            Assert.False(FromXml(future).EnableEpubCoverExtraction);
        }
    }
}
