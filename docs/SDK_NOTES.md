# SDK_NOTES — BookMeta

> Conclusions sur le SDK Emby **4.10.2.0** (extrait de `/system/` du pod `emby2`, le 2026-10-30), obtenues par réflexion
> (`System.Reflection.Metadata`) et lecture IL des DLL réelles — jamais supposées depuis Jellyfin.
> Assemblies publiques référencées (`libs/`, `Private=false`) : `MediaBrowser.Common`, `MediaBrowser.Controller`, `MediaBrowser.Model`,
> toutes en **4.10.2.0**. Les assemblies `Emby.*` (Providers, Server.Implementations…) ont été lues pour comprendre le comportement du
> serveur mais ne sont **jamais référencées ni committées**. Les `libs/` de `../Emby_Badges` sont en 4.9.3.0 : ne pas les utiliser.

Légende : `[Common]` = MediaBrowser.Common, `[Controller]` = MediaBrowser.Controller, `[Model]` = MediaBrowser.Model (tous 4.10.2.0).

## 1. Entité `Book`

`[Controller] MediaBrowser.Controller.Entities.Book : BaseItem` — implémente `IHasProviderIds`, `IHasLookupInfo<ItemLookupInfo>`,
`IHasLookupInfo<BookInfo>` ; `MediaType = "Book"`.

- Surcharges : `SupportsStudios = true`, `SupportsGenres = true`, `IsMedia = true`, `ParseNameBeforeMetadataRefresh = true`.
- **`SupportsPeople` n'est pas surchargée** : valeur `BaseItem` = `false` (IL du getter : `ldc.i4.0; ret`). `DtoService.AttachPeople`
  (`Emby.Server.Implementations`) teste `item.SupportsPeople` avant de charger les personnes → **aucune personne n'est jamais renvoyée au
  client pour un livre**, quoi que `MetadataResult.People` contienne. Il n'existe pas de `PersonType` « Author »
  (`Actor, Director, Writer, Producer, GuestStar, Composer, Conductor, Lyricist`).
- **Aucune notion de série** : `Book` n'implémente ni `IHasSeries` ni `IHasSeriesName` (réservés à `Episode`/`Season`) ;
  `BookInfo.SeriesName` n'est jamais renseigné ; `BaseItemDto.SeriesName` ne vient que de `Episode`/`Season`.
- Propriétés `BaseItem` utilisées : `Name`, `SortName` (le setter applique `RemoveDiacritics`), `Overview`, `PremiereDate : DateTimeOffset?`,
  `ProductionYear : int?`, `Studios : string[]` (`SetStudios`), `Genres : string[]` (`AddGenre`), `Tags : string[]`,
  `ProviderIds : ProviderIdDictionary` (`SetProviderId(string, string)`), `Path`, `ContainingFolderPath`, `DateLastRefreshed`.
  `IndexNumber` volontairement non renseigné (affiché comme un numéro d'épisode).
- `[Model] MetadataProviders.ISBN = 20` ; `Emby.Providers.ExternalIds.ISBNExternalId` (clé `ISBN`) fournit le lien externe.
- Résolution (`Emby.Server.Implementations…Books.BookResolver`) : `.pdf .epub .mobi .cbr .cbz .azw3 .fb2 .fbz`, **un item par fichier**
  (`Frankenwood (2026)/` avec pdf + cbz → deux livres alimentés par le même `metadata.opf`). Le provider natif `BookProvider`
  (`ICustomMetadataProvider<Book>`, `IForcedProvider`) ne fait que `Container` et `DateModified` : **Emby ne lit aucun OPF**.

## 2. Interfaces de providers (`[Controller] MediaBrowser.Controller.Providers`)

| Type | Signature utilisée par BookMeta |
|------|---------------------------------|
| `IMetadataProvider` | `string Name { get; }` — **clé des réglages de bibliothèque** (voir §4) |
| `ILocalMetadataProvider<T>` | `Task<MetadataResult<T>> GetMetadata(ItemInfo info, LibraryOptions libraryOptions, IDirectoryService directoryService, CancellationToken cancellationToken)` |
| `IHasItemChangeMonitor` | `bool HasChanged(BaseItem item, LibraryOptions libraryOptions, IDirectoryService directoryService)` |
| `IHasOrder` | `int Order { get; }` — **non implémenté** : l'ordre des lecteurs vient des réglages de bibliothèque (`LocalMetadataReaderOrder`) |
| `ItemInfo` | `Id`, `Path`, `Name`, `ContainingFolderPath`, `Container`, `IsInMixedFolder` |
| `MetadataResult<T> : BaseMetadataResult` | `T Item`, `HasMetadata`, `People`, `Images`, `ResultLanguage`, `Provider`, `BaseItem` |
| `IImageProvider` | `string Name { get; }`, `bool Supports(BaseItem item)` |
| `ILocalImageFileProvider : ILocalImageProvider, IImageProvider` | `List<LocalImageInfo> GetImages(BaseItem item, LibraryOptions libraryOptions, IDirectoryService directoryService, CancellationToken cancellationToken)` |
| `LocalImageInfo` | `FileSystemMetadata FileInfo`, `ImageType Type` |
| `IDynamicImageProvider : IImageProvider` | `ImageType[] GetSupportedImages(BaseItem item)` ; `Task<DynamicImageResponse> GetImage(BaseMetadataResult itemResult, ImageType type, CancellationToken cancellationToken)` — ⚠ reçoit un **`BaseMetadataResult`** (l'item = `.BaseItem`), pas un `BaseItem` |
| `IDynamicImageProviderWithLibraryOptions` | variante `GetImage(BaseMetadataResult, BaseItem[] collectionFolders, LibraryOptions, ImageType, CancellationToken)` — non utilisée en v1 |
| `DynamicImageResponse` | `Path`, `Protocol`, `Stream`, `Format : ImageFormat`, `GeneratedFromItemIds`, `SetFormatFromMimeType(string)` |
| `IDirectoryService` | `GetFiles(string)`, `GetFile(string)`, `GetFile(string dir, string name, bool caseSensitive)`, `GetFileSystemEntries(string)`, `GetFilePaths(string)` |

Autres types : `[Model] MediaBrowser.Model.Drawing.ImageFormat { Bmp, Gif, Jpg, Png, Webp, Avif }` ; `ImageType.Primary = 0` ;
`[Model] MediaBrowser.Model.IO.FileSystemMetadata { Exists, FullName, Name, Extension, Length, DirectoryName, LastWriteTimeUtc, IsDirectory… }` ;
`[Model] IFileSystem` (`GetFileInfo`, `OpenRead`, `FileExists`, `GetLastWriteTimeUtc`…) ;
`[Model] MediaBrowser.Model.Logging.ILogger` (`Info/Warn/Error/Debug(string, params object[])`, `ErrorException(string, Exception, params object[])`) et
`ILogManager.GetLogger(string)` — injection par constructeur.

### Points ouverts du handoff — résultat de la re-vérification

| Point | Résultat | Statut |
|-------|----------|--------|
| Signature de `IDynamicImageProvider.GetImage` | Confirmée par réflexion : `BaseMetadataResult` en 1er paramètre. `ItemImageProvider.RefreshFromProvider` appelle `IDynamicImageProvider.GetSupportedImages` puis `GetImage` (ou la variante `…WithLibraryOptions` si implémentée) | **Tranché** |
| « Pas d'image » côté `DynamicImageResponse` | L'IL de `ItemImageProvider` lit `DynamicImageResponse.Path`, `Stream`, `Format`, `Protocol`, `GeneratedFromItemIds` ; erreur de provider journalisée « Error in {0} dynamic image provider ». Retourner `null` ou une réponse sans `Stream` ni `Path` est le cas attendu | **À vérifier en LOT-2 sur emby2** (retourner une réponse vide, constater l'absence d'exception) |
| Comportement d'Emby si `HasMetadata = false` | Non lu dans l'IL | **À vérifier en LOT-2 sur emby2** |
| `ItemInfo.ContainingFolderPath` en dossier mixte | Non lu dans l'IL ; BookMeta ne s'en sert pas : `OpfLocator` dérive le dossier de `ItemInfo.Path` | **À vérifier en LOT-2 sur emby2** |

## 3. Plugin et configuration

- `[Common] MediaBrowser.Common.Plugins.BasePlugin<TConfig>` (ctor `IApplicationPaths`, `IXmlSerializer`) : `Id`, `Name`, `Description`,
  `Configuration`, `SaveConfiguration()` ; `BasePluginConfiguration` en `[Model] MediaBrowser.Model.Plugins`.
- `IHasWebPages.GetPages() → PluginPageInfo { Name, DisplayName, EmbeddedResourcePath, EnableInMainMenu, MenuSection, MenuIcon }`.
- `IHasThumbImage` (`GetThumbImage`, `ThumbImageFormat`) : optionnel, non implémenté en v0.1.0.
- Page : fragment HTML `data-controller="__plugin/BookMetaConfigScript"` + module AMD ; lecture/écriture par
  `ApiClient.getPluginConfiguration(PLUGIN_ID)` / `updatePluginConfiguration` — API standard, aucun endpoint propre au plugin.
- **GUID du plugin** : `097ed1ba-f0ae-496f-8f56-e54599fe54b1` (`Plugin.PluginId`, repris dans `configScript.js`) — **jamais modifié**.

## 4. Sélection des providers par Emby

`ProviderManager.CanRefresh` teste `IsLocalMetadataReaderEnabled(libraryOptions, provider.Name)` (lecteurs locaux) et
`IsImageFetcherEnabled(libraryOptions, provider.Name)` (images) : le **`Name` est donc la clé stockée dans les réglages de bibliothèque**.
L'ordre vient de `LibraryOptions.LocalMetadataReaderOrder` / `TypeOptions.ImageFetcherOrder`, puis `IHasOrder.Order`.
Noms figés (voir `contracts/models.md` §6) : `BookMeta`, `BookMeta Cover File`, `BookMeta Embedded Cover` — les renommer est BREAKING.

## 5. Décisions GATE 2 (validées par l'utilisateur)

| Sujet | Décision |
|-------|----------|
| Auteur | **Tag** au nom de l'auteur (un par auteur) + `SortName = "<Nom, Prénom> - <titre de tri>"` ; `People` **non renseignés** (inutiles et invisibles, cf. §1) |
| Série | **Tag** « Série : X » + `SortName = "<Auteur> - <Série> <NN> - <titre de tri>"` ; `IndexNumber` non renseigné |
| Release à blanc | `workflow_dispatch` dans `release.yml` (tâche infra, #3) |

## 6. Architecture figée (`src/BookMeta/`)

```
BookMeta.sln
src/BookMeta/
├── BookMeta.csproj            net6.0, AssemblyName BookMeta, Version 0.1.0.0, refs libs/*.dll Private=false, InternalsVisibleTo BookMeta.Tests
├── Plugin.cs                  BasePlugin<PluginConfiguration>, IHasWebPages ; Id = PluginId ; Name "BookMeta"            [livré #1]
├── Configuration/             PluginConfiguration (EnableEpubCoverExtraction = true), configPage.html, configScript.js   [coquille #1, finalisée #8]
├── Opf/                       (aucune dépendance Emby — testable à 100 % en xUnit)                                        [LOT-2, tâche 6]
│   ├── OpfDocument.cs         modèle immuable (titre, tri, auteurs, description, date, langue, éditeur, sujets, série, identifiants, manifest)
│   ├── OpfParser.cs           XDocument sécurisé (DtdProcessing.Ignore, XmlResolver = null), OPF 2 et 3, tolérant
│   ├── OpfLocator.cs          metadata.opf du dossier (IDirectoryService), sinon OPF interne de l'epub
│   └── HtmlText.cs            description → texte brut (entités, <br>/<p> → \n, balises retirées)
├── Epub/EpubArchive.cs        ZipArchive lecture : container.xml → rootfile, OpenOpf, ResolveHref, OpenEntry
├── Covers/                    CoverFileLocator, EpubCoverExtractor, CbzCoverExtractor, ImageFormats                       [LOT-2, tâche 9]
└── Providers/                 BookMetadataProvider (ILocalMetadataProvider<Book>, IHasItemChangeMonitor), BookMapper (pur),
                               BookCoverFileProvider (ILocalImageFileProvider), BookEmbeddedCoverProvider (IDynamicImageProvider)
tests/BookMeta.Tests/          xUnit 2.6.2 ; Unit/ (dev-plugin), Integration/<Theme>/<Lot>/ (test-writer), Fixtures/
```

Flux `GetMetadata` : `OpfLocator` (dossier → epub) → `OpfParser` → `BookMapper` → `MetadataResult<Book>`. Toute exception est attrapée au
niveau du provider (`Warn` + `HasMetadata = false`). Aucun accès réseau, aucune écriture disque.

## 7. Références

Contrat de mapping : `contracts/models.md`. Cadrage : `docs/CADRAGE.md`. Docs XML officielles du SDK (non committées) : `/system/MediaBrowser.{Common,Controller,Model}.xml` du pod `emby2`
(`kubectl -n media cp <pod-emby2>:/system/<fichier> …`, `KUBECONFIG=private/kubeconfig.yml`) ; les DLL `libs/` viennent du même dossier.
