using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using BookEntity = MediaBrowser.Controller.Entities.Book;

namespace BookMeta.Tests.Support
{
    /// <summary>
    /// SEUL point de contact entre les tests de spécification LOT-2 et le code de production (BookMeta.Providers).
    /// Les noms de types/constructeurs ci-dessous sont ceux de l'architecture figée (docs/SDK_NOTES.md §architecture) :
    /// à confronter au handoff dev-plugin ; en cas d'écart, corriger ICI uniquement.
    /// </summary>
    public static class SpecApi
    {
#if LOT2_IMPL
        public static bool Implemented => true;

        private static BookEntity NewBook(string bookPath)
        {
            // BaseItem.ContainingFolderPath / SortName lisent des états statiques posés par Emby au démarrage.
            BaseItem.FileSystem = RealFs.FileSystem();
            BaseItem.LocalizationManager = StubProxy.Create<MediaBrowser.Model.Globalization.ILocalizationManager>(
                (m, a) => m.Name == "RemoveDiacritics" ? a[0] : null);
            return new BookEntity { Path = bookPath, Name = System.IO.Path.GetFileNameWithoutExtension(bookPath) };
        }

        public static MetadataResult<BookEntity> GetMetadata(string bookPath, RecordingLogger log)
        {
            // ASSUMPTION : ctor (ILogManager, IFileSystem) — cf. SDK_NOTES « injection par constructeur ».
            var provider = new global::BookMeta.Providers.BookMetadataProvider(log.Manager, RealFs.FileSystem());
            var info = new ItemInfo(NewBook(bookPath));
            return provider.GetMetadata(info, new LibraryOptions(), RealFs.DirectoryService(), CancellationToken.None)
                .GetAwaiter().GetResult();
        }

        public static bool HasChanged(string bookPath, DateTime dateLastRefreshedUtc, RecordingLogger log)
        {
            var provider = new global::BookMeta.Providers.BookMetadataProvider(log.Manager, RealFs.FileSystem());
            var book = NewBook(bookPath);
            book.DateLastRefreshed = dateLastRefreshedUtc;
            return provider.HasChanged(book, new LibraryOptions(), RealFs.DirectoryService());
        }

        public static string MetadataProviderName(RecordingLogger log) =>
            new global::BookMeta.Providers.BookMetadataProvider(log.Manager, RealFs.FileSystem()).Name;

        public static List<LocalImageInfo> GetFolderCovers(string bookPath, RecordingLogger log)
        {
            var provider = new global::BookMeta.Providers.BookCoverFileProvider(log.Manager);
            return provider.GetImages(NewBook(bookPath), new LibraryOptions(), RealFs.DirectoryService(), CancellationToken.None);
        }

        public static bool SupportsFolderCovers(BaseItem item, RecordingLogger log) =>
            new global::BookMeta.Providers.BookCoverFileProvider(log.Manager).Supports(item);

        public static string FolderCoverProviderName(RecordingLogger log) =>
            new global::BookMeta.Providers.BookCoverFileProvider(log.Manager).Name;

        /// <summary>Renvoie la réponse du fournisseur d'image embarquée (null accepté = « pas d'image »).</summary>
        public static DynamicImageResponse? GetEmbeddedCover(string bookPath, RecordingLogger log)
        {
            var provider = new global::BookMeta.Providers.BookEmbeddedCoverProvider(log.Manager, RealFs.FileSystem());
            var result = new MetadataResult<BookEntity> { Item = NewBook(bookPath) };
            return provider.GetImage(result, ImageType.Primary, CancellationToken.None).GetAwaiter().GetResult();
        }

        public static bool SupportsEmbeddedCover(BaseItem item, RecordingLogger log) =>
            new global::BookMeta.Providers.BookEmbeddedCoverProvider(log.Manager, RealFs.FileSystem()).Supports(item);

        public static string EmbeddedCoverProviderName(RecordingLogger log) =>
            new global::BookMeta.Providers.BookEmbeddedCoverProvider(log.Manager, RealFs.FileSystem()).Name;

        public static ImageType[] EmbeddedSupportedImages(string bookPath, RecordingLogger log) =>
            new global::BookMeta.Providers.BookEmbeddedCoverProvider(log.Manager, RealFs.FileSystem())
                .GetSupportedImages(NewBook(bookPath));
#else
        public static bool Implemented => false;

        private static Exception NotWired() => new NotImplementedException("SpecApi non branché : dotnet test -p:Lot2Specs=true");

        public static MetadataResult<BookEntity> GetMetadata(string bookPath, RecordingLogger log) => throw NotWired();
        public static bool HasChanged(string bookPath, DateTime dateLastRefreshedUtc, RecordingLogger log) => throw NotWired();
        public static string MetadataProviderName(RecordingLogger log) => throw NotWired();
        public static List<LocalImageInfo> GetFolderCovers(string bookPath, RecordingLogger log) => throw NotWired();
        public static bool SupportsFolderCovers(BaseItem item, RecordingLogger log) => throw NotWired();
        public static string FolderCoverProviderName(RecordingLogger log) => throw NotWired();
        public static DynamicImageResponse? GetEmbeddedCover(string bookPath, RecordingLogger log) => throw NotWired();
        public static bool SupportsEmbeddedCover(BaseItem item, RecordingLogger log) => throw NotWired();
        public static string EmbeddedCoverProviderName(RecordingLogger log) => throw NotWired();
        public static ImageType[] EmbeddedSupportedImages(string bookPath, RecordingLogger log) => throw NotWired();
#endif

        /// <summary>Vrai si la réponse porte une image (Stream ou Path) ; « pas d'image » = null, ou Stream et Path vides.</summary>
        public static bool HasImage(DynamicImageResponse? r) => r != null && (r.Stream != null || !string.IsNullOrEmpty(r.Path));

        public static byte[] ReadImage(DynamicImageResponse r)
        {
            if (r.Stream != null)
            {
                using var ms = new MemoryStream();
                if (r.Stream.CanSeek) r.Stream.Position = 0;
                r.Stream.CopyTo(ms);
                return ms.ToArray();
            }
            return File.ReadAllBytes(r.Path);
        }
    }
}
