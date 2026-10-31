using BookMeta.Tests.Support;
using Xunit;

// Lot : Integration/Opf/Epub — #6 : repli sur l'OPF interne de l'epub (META-INF/container.xml → rootfile), contrat §1 priorité 2.
namespace BookMeta.Tests.Integration.Opf.Epub
{
    [Trait("Lot", "Integration/Opf/Epub")]
    public class EpubInternalOpfTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private static string RichOpf2() =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><package xmlns=\"http://www.idpf.org/2007/opf\" version=\"2.0\" unique-identifier=\"uid\">" +
            "<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:opf=\"http://www.idpf.org/2007/opf\">" +
            "<dc:title>Nuits d'enfer au Paradis</dc:title><dc:creator opf:role=\"aut\" opf:file-as=\"Meyer, Stephenie\">Stephenie Meyer</dc:creator>" +
            "<dc:identifier id=\"uid\">x</dc:identifier><dc:language>fr</dc:language><dc:date>2009-06-01</dc:date>" +
            "<dc:description>Un r&#233;sum&#233; interne.</dc:description></metadata><manifest/><spine/></package>";

        [SpecFact]
        public void Sans_sidecar_l_opf_interne_renseigne_le_livre()
        {
            var dir = _fs.Dir("Stephenie Meyer", "Nuits D'enfer Au Paradis", 2009);
            var epub = Path.Combine(dir, "Nuits D'enfer Au Paradis - Stephenie Meyer.epub");
            Archives.WriteEpub(epub, RichOpf2());

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.True(r.HasMetadata);
            Assert.Equal("Nuits d'enfer au Paradis", r.Item.Name);
            Assert.Contains("Stephenie Meyer", r.Item.Tags);
            Assert.Equal(2009, r.Item.ProductionYear);
            Assert.Contains("résumé interne", r.Item.Overview);
        }

        [SpecFact]
        public void Le_rootfile_peut_etre_dans_un_sous_dossier_quelconque()
        {
            var dir = _fs.Dir("Auteur Test", "Rootfile profond", 2020);
            var epub = Path.Combine(dir, "Rootfile profond.epub");
            Archives.WriteEpub(epub, RichOpf2(), opfPath: "OEBPS/livre/Content/package.opf");

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.True(r.HasMetadata);
            Assert.Equal("Nuits d'enfer au Paradis", r.Item.Name);
        }

        [SpecFact]
        public void Le_rootfile_a_la_racine_de_l_archive_est_lu()
        {
            var dir = _fs.Dir("Auteur Test", "Rootfile racine", 2020);
            var epub = Path.Combine(dir, "Rootfile racine.epub");
            Archives.WriteEpub(epub, RichOpf2(), opfPath: "content.opf");

            Assert.True(SpecApi.GetMetadata(epub, _log).HasMetadata);
        }

        [SpecFact]
        public void Epub_sans_container_xml_donne_HasMetadata_faux_sans_exception()
        {
            var dir = _fs.Dir("Auteur Test", "Sans container", 2020);
            var epub = Path.Combine(dir, "Sans container.epub");
            Archives.WriteEpub(epub, RichOpf2(), withContainer: false);

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.False(r.HasMetadata);
        }

        [SpecFact]
        public void Container_pointant_vers_un_rootfile_absent_donne_HasMetadata_faux()
        {
            var dir = _fs.Dir("Auteur Test", "Rootfile absent", 2020);
            var epub = Path.Combine(dir, "Rootfile absent.epub");
            Archives.WriteZip(epub,
                Archives.Text("META-INF/container.xml", string.Format(Archives.ContainerXml, "OEBPS/inexistant.opf")));

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.False(r.HasMetadata);
            Assert.True(_log.HasWarning);
        }

        [SpecFact]
        public void Sidecar_invalide_puis_repli_sur_l_opf_interne()
        {
            var dir = _fs.Dir("Auteur Test", "Repli", 2020);
            BookFolder.PlaceSidecar(dir, "invalid.opf");
            var epub = Path.Combine(dir, "Repli.epub");
            Archives.WriteEpub(epub, RichOpf2());

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.True(r.HasMetadata);
            Assert.Equal("Nuits d'enfer au Paradis", r.Item.Name);
            Assert.True(_log.HasWarning, "le sidecar illisible doit être signalé en Warn");
        }

        [SpecFact]
        public void Sidecar_sans_titre_puis_repli_sur_l_opf_interne()
        {
            var dir = _fs.Dir("Auteur Test", "Repli sans titre", 2020);
            BookFolder.PlaceSidecar(dir, "no-title.opf");
            var epub = Path.Combine(dir, "Repli sans titre.epub");
            Archives.WriteEpub(epub, RichOpf2());

            Assert.Equal("Nuits d'enfer au Paradis", SpecApi.GetMetadata(epub, _log).Item.Name);
        }

        [SpecFact]
        public void Un_livre_non_epub_sans_sidecar_n_a_pas_d_opf_interne()
        {
            var dir = _fs.Dir("Auteur Test", "Un cbz", 2020);
            var cbz = Path.Combine(dir, "Un cbz.cbz");
            Archives.WriteZip(cbz, ("001.jpg", Img.Jpg("a")));

            Assert.False(SpecApi.GetMetadata(cbz, _log).HasMetadata);
        }

        [SpecFact]
        public void Epub_OPF3_interne_est_lu()
        {
            var dir = _fs.Dir("Auteur Test", "Opf3 interne", 2020);
            var epub = Path.Combine(dir, "Opf3 interne.epub");
            Archives.WriteEpub(epub, BookFolder.FixtureText("opf3-sample.opf"));

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.True(r.HasMetadata);
            Assert.Equal("Le Nom du Vent", r.Item.Name);
        }
    }
}
