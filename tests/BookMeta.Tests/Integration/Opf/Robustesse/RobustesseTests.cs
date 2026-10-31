using System.Text;
using BookMeta.Tests.Support;
using Xunit;

// Lot : Integration/Opf/Robustesse — contrat §1 : OPF/archives illisibles → Warn, jamais d'exception ; XML hostile neutralisé.
namespace BookMeta.Tests.Integration.Opf.Robustesse
{
    [Trait("Lot", "Integration/Opf/Robustesse")]
    public class RobustesseTests : IDisposable
    {
        private readonly BookFolder _fs = new();
        private readonly RecordingLogger _log = new();

        public void Dispose() => _fs.Dispose();

        private string Book(string title = "Livre") =>
            BookFolder.Touch(_fs.Dir("Auteur Test", title, 2020), title + ".epub");

        [SpecFact]
        public void Opf_malforme_HasMetadata_faux_Warn_et_pas_d_exception()
        {
            var book = Book();
            BookFolder.PlaceSidecar(Path.GetDirectoryName(book)!, "invalid.opf");

            var r = SpecApi.GetMetadata(book, _log);

            Assert.False(r.HasMetadata);
            Assert.True(_log.HasWarning);
            Assert.Contains(_log.Warnings, w => w.Contains("metadata.opf"));
        }

        [SpecFact]
        public void Opf_vide_ne_leve_pas_d_exception()
        {
            var book = Book();
            BookFolder.WriteSidecarText(Path.GetDirectoryName(book)!, "");

            Assert.False(SpecApi.GetMetadata(book, _log).HasMetadata);
        }

        [SpecFact]
        public void Opf_binaire_ne_leve_pas_d_exception()
        {
            var book = Book();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(book)!, "metadata.opf"), new byte[] { 0, 1, 2, 0xFF, 0xFE, 0x00, 0x80 });

            Assert.False(SpecApi.GetMetadata(book, _log).HasMetadata);
        }

        [SpecFact]
        public void Opf_sans_titre_donne_HasMetadata_faux()
        {
            var book = Book();
            BookFolder.PlaceSidecar(Path.GetDirectoryName(book)!, "no-title.opf");

            Assert.False(SpecApi.GetMetadata(book, _log).HasMetadata);
        }

        [SpecFact]
        public void Opf_minimal_titre_seul_les_autres_champs_restent_vides()
        {
            var book = Book();
            BookFolder.PlaceSidecar(Path.GetDirectoryName(book)!, "minimal.opf");

            var r = SpecApi.GetMetadata(book, _log);

            Assert.True(r.HasMetadata);
            Assert.Equal("Seulement un titre", r.Item.Name);
            Assert.Null(r.Item.Overview);
            Assert.Null(r.Item.PremiereDate);
            Assert.Null(r.Item.ProductionYear);
            Assert.Empty(r.Item.Tags ?? Array.Empty<string>());
            Assert.Empty(r.Item.Studios ?? Array.Empty<string>());
            Assert.Empty(r.Item.Genres ?? Array.Empty<string>());
        }

        [SpecFact]
        public void Epub_corrompu_ne_leve_pas_d_exception()
        {
            var dir = _fs.Dir("Auteur Test", "Corrompu", 2020);
            var epub = BookFolder.Touch(dir, "Corrompu.epub", Encoding.ASCII.GetBytes("ceci n'est pas une archive zip"));

            var r = SpecApi.GetMetadata(epub, _log);

            Assert.False(r.HasMetadata);
            Assert.True(_log.HasWarning);
        }

        [SpecFact]
        public void Epub_vide_ne_leve_pas_d_exception()
        {
            var dir = _fs.Dir("Auteur Test", "Vide", 2020);
            var epub = BookFolder.Touch(dir, "Vide.epub", Array.Empty<byte>());

            Assert.False(SpecApi.GetMetadata(epub, _log).HasMetadata);
        }

        [SpecFact]
        public void Entite_externe_XXE_n_est_pas_resolue_et_le_titre_est_conserve()
        {
            var book = Book();
            BookFolder.PlaceSidecar(Path.GetDirectoryName(book)!, "xxe.opf");

            var r = SpecApi.GetMetadata(book, _log);

            // Soit l'OPF est refusé, soit il est lu SANS résoudre l'entité : dans aucun cas le contenu du fichier cible n'apparaît.
            if (r.HasMetadata)
            {
                Assert.Equal("Livre hostile", r.Item.Name);
                Assert.DoesNotContain("root:", r.Item.Overview ?? "");
            }
        }

        [SpecFact]
        public void Opf_encode_avec_bom_utf8_est_lu()
        {
            var book = Book();
            var dir = Path.GetDirectoryName(book)!;
            File.WriteAllText(Path.Combine(dir, "metadata.opf"), BookFolder.FixtureText("tentation-bindery.opf"), new UTF8Encoding(true));

            Assert.Equal("Tentation", SpecApi.GetMetadata(book, _log).Item.Name);
        }

        [SpecFact]
        public void Un_dossier_inexistant_ne_leve_pas_d_exception()
        {
            var absent = Path.Combine(_fs.Root, "N'existe pas (2020)", "Livre.epub");

            var r = SpecApi.GetMetadata(absent, _log);

            Assert.False(r.HasMetadata);
        }
    }
}
