using BookMeta.Tests.Support;
using Xunit;

// Lot : Integration/Opf/Sidecar (tag critical) — #6 : metadata.opf Bindery dans le dossier du livre (contracts/models.md §1, §2, §3).
namespace BookMeta.Tests.Integration.Opf.Sidecar
{
    [Trait("Tag", "critical")]
    [Trait("Lot", "Integration/Opf/Sidecar")]
    public class SidecarTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private string TentationDir()
        {
            var dir = _fs.Dir("Stephenie Meyer", "Tentation", 2006);
            BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");
            return dir;
        }

        [SpecFact]
        public void Tentation_est_renseignee_depuis_le_metadata_opf()
        {
            var dir = TentationDir();
            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub"), _log);

            Assert.True(r.HasMetadata);
            Assert.Equal("Tentation", r.Item.Name);
        }

        [SpecFact]
        public void Tentation_SortName_est_auteur_file_as_puis_titre_de_tri()
        {
            var dir = TentationDir();
            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub"), _log);

            Assert.Equal("Meyer, Stephenie - Tentation", r.Item.SortName);
        }

        [SpecFact]
        public void Tentation_auteur_expose_en_tag_et_aucune_personne()
        {
            var dir = TentationDir();
            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub"), _log);

            Assert.Contains("Stephenie Meyer", r.Item.Tags);
            Assert.Empty(r.People ?? new List<MediaBrowser.Controller.Entities.PersonInfo>());
        }

        [SpecFact]
        public void Tentation_resume_et_date()
        {
            var dir = TentationDir();
            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub"), _log);

            Assert.Contains("Bella ne s en relève pas", r.Item.Overview);
            Assert.Equal(2006, r.Item.ProductionYear);
            Assert.Equal(new DateTime(2006, 1, 1), r.Item.PremiereDate!.Value.UtcDateTime.Date);
        }

        [SpecFact]
        public void Tentation_identifiants_BNF_et_BINDERY()
        {
            var dir = TentationDir();
            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub"), _log);

            Assert.Equal("cb43546026q", r.Item.ProviderIds["BNF"]);
            Assert.Equal("84", r.Item.ProviderIds["BINDERY"]);
        }

        [SpecFact]
        public void Tentation_langue_et_provider_du_resultat()
        {
            var dir = TentationDir();
            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub"), _log);

            Assert.Equal("fr", r.ResultLanguage);
            Assert.Equal("BookMeta", r.Provider);
        }

        [SpecFact]
        public void Le_livre_pdf_du_meme_dossier_recoit_aussi_les_metadonnees()
        {
            // Cas Frankenwood : .pdf + .cbz + un seul metadata.opf → les deux items sont renseignés.
            var dir = _fs.Dir("Darko Macan", "Frankenwood", 2026);
            BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");

            var pdf = SpecApi.GetMetadata(BookFolder.Touch(dir, "Frankenwood - Darko Macan.pdf"), _log);
            var cbz = SpecApi.GetMetadata(BookFolder.Touch(dir, "Frankenwood - Darko Macan.cbz"), _log);

            Assert.True(pdf.HasMetadata);
            Assert.True(cbz.HasMetadata);
        }

        [SpecFact]
        public void Le_metadata_opf_prime_sur_l_opf_interne_de_l_epub()
        {
            var dir = TentationDir();
            var epub = Path.Combine(dir, "Tentation - Stephenie Meyer.epub");
            Archives.WriteEpub(epub, Archives.Opf2("Titre interne différent", null));

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.Equal("Tentation", r.Item.Name);
        }

        [SpecFact]
        public void Dossier_avec_apostrophe_et_accents()
        {
            var dir = _fs.Dir("Stéphanie D'Été", "L'appel du sang", 2010);
            BookFolder.PlaceSidecar(dir, "tentation-bindery.opf");

            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "L'appel du sang - Hésitation.epub"), _log);

            Assert.True(r.HasMetadata);
            Assert.Equal("Tentation", r.Item.Name);
        }

        [SpecFact]
        public void Sans_aucune_source_HasMetadata_est_faux_sans_warning()
        {
            var dir = _fs.Dir("Auteur Test", "Rien", 2020);

            var r = SpecApi.GetMetadata(BookFolder.Touch(dir, "Rien.pdf"), _log);

            Assert.False(r.HasMetadata);
            Assert.False(_log.HasWarning);
        }

        [SpecFact]
        public void Le_provider_garde_son_nom_stable()
        {
            Assert.Equal("BookMeta", SpecApi.MetadataProviderName(_log));
        }

        // --- Relecture (IHasItemChangeMonitor, contrat §3) ---

        [SpecFact]
        public void HasChanged_vrai_si_l_opf_est_plus_recent_que_la_derniere_actualisation()
        {
            var dir = TentationDir();
            var book = BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub");
            File.SetLastWriteTimeUtc(Path.Combine(dir, "metadata.opf"), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.True(SpecApi.HasChanged(book, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), _log));
        }

        [SpecFact]
        public void HasChanged_faux_si_l_opf_est_plus_ancien_que_la_derniere_actualisation()
        {
            var dir = TentationDir();
            var book = BookFolder.Touch(dir, "Tentation - Stephenie Meyer.epub");
            File.SetLastWriteTimeUtc(Path.Combine(dir, "metadata.opf"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            File.SetLastWriteTimeUtc(book, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.False(SpecApi.HasChanged(book, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), _log));
        }

        [SpecFact]
        public void HasChanged_sans_opf_se_rabat_sur_la_date_du_fichier_livre()
        {
            var dir = _fs.Dir("Auteur Test", "Sans OPF", 2020);
            var book = BookFolder.Touch(dir, "Sans OPF.epub");
            File.SetLastWriteTimeUtc(book, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.True(SpecApi.HasChanged(book, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), _log));
        }

        [SpecFact]
        public void HasChanged_faux_et_sans_exception_si_le_fichier_est_absent()
        {
            var dir = _fs.Dir("Auteur Test", "Fantome", 2020);
            var absent = Path.Combine(dir, "Fantome.epub");

            Assert.False(SpecApi.HasChanged(absent, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), _log));
        }
    }
}
