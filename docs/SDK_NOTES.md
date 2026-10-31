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
| « Pas d'image » côté `DynamicImageResponse` | Tranché par l'IL de `ItemImageProvider.RefreshFromProvider` : après `GetImage`, Emby lit `response.Path` (non vide → `SetImage`), sinon `response.Stream` (non nul → `SaveImage` avec `image/<Format>`). **Une réponse vide (ni `Path` ni `Stream`) ne déclenche aucune branche** ; une réponse `null` lève une `NullReferenceException` attrapée par « Error in {0} dynamic image provider » et marque le refresh en erreur → BookMeta renvoie toujours une réponse vide | **Tranché** (IL) ; confirmation visuelle en recette emby2 |
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

## 8. Défauts de recette emby2 (cycle 3) — mécanismes établis dans l'IL 4.10.2

### D1 — le `SortName` d'un lecteur local est effacé après chaque actualisation
- `ProviderUtils.MergeBaseItemData` copie bien `SortName` du résultat vers l'item (si `replaceData` ou tri vide, champ non verrouillé).
- Mais `BaseItem.AfterMetadataRefresh` appelle `ResetSortName()` : `if (!IsFieldLocked(MetadataFields.SortName)) _sortName = null;`
  → le getter recalcule alors le tri depuis `Name` (`CreateSortName` : retire les articles « La/Le… » via `ILocalizationManager.TrimSortRemoveWords`, d'où `La psy` → `psy`).
  Constaté en recette : 17/17 livres avec le tri par défaut d'Emby.
- `MetadataService.MergeData` ne copie pas `LockedFields` du résultat ; `CopyMetadataSettings` les copie de l'item réel vers l'item temporaire.
- **Premier correctif (cycle 3, insuffisant)** : verrouiller l'item retrouvé par `BaseItem.LibraryManager.GetItemById(info.Id)`. Constaté en recette : l'item trouvé est une **autre instance** que celle rafraîchie et sauvegardée (`LockedFields` vide ensuite) → retiré. Voir §9.

### D2 — « BookMeta Embedded Cover » n'apparaît pas dans les récupérateurs d'images
- La liste des récupérateurs d'une bibliothèque (`Libraries/AvailableOptions`) est construite avec un livre factice **sans `Path`** ; un `Supports(item)` qui exige un chemin masque le provider (constaté : `ImageFetchers = []`).
  → `Supports` renvoie `true` pour tout `Book` sans chemin ; avec chemin : `.cbz`, ou `.epub` si l'option est active ; le reste est contrôlé dans `GetImage` (réponse vide).
- **Activation** : `BaseItem.IsImageFetcherEnabled` exige que le nom soit dans `TypeOptions.ImageFetchers` de la bibliothèque ; `ProviderManager.IsImageFetcherEnabledByDefault` est une **liste figée de noms Emby** (« Embedded Images », « Screen Grabber »…) qui ne contient pas BookMeta.
  → après installation, l'utilisateur doit **cocher « BookMeta Embedded Cover »** dans Réglages de la bibliothèque → Récupérateurs d'images (à documenter au README et à la recette), ou l'ajouter via l'API (`LibraryOptions.TypeOptions[Book].ImageFetchers`).
- `BookMeta Cover File` est un `ILocalImageFileProvider` (images locales) : il n'apparaît pas dans cette liste et n'a pas besoin d'être activé.

## 9. D1 (cycle 4) — verrouiller le tri sur l'instance rafraîchie : `ICustomMetadataProvider<Book>`

Chaîne d'exécution lue dans `Emby.Providers.Manager.MetadataService<,>` (IL 4.10.2) :
`RefreshMetadata` → `BaseItem.BeforeMetadataRefresh` → `RefreshWithProviders` { `RunCustomProvider` des providers « pré-rafraîchissement » (`IPreRefreshProvider`) → lecteurs locaux (`GetItemsFromLocalProvider`, fusion `MergeData`) → providers distants (`ExecuteRemoteProviders`) → `RunCustomProvider` des providers « custom » restants, **en fin de chaîne** } → images → `MetadataService.AfterMetadataRefresh` (= `BaseItem.AfterMetadataRefresh` → `ResetSortName`) → `SaveItem` (`UpdateToRepository`).

- `ICustomMetadataProvider<Book>.FetchAsync(MetadataResult<Book> itemResult, MetadataRefreshOptions, LibraryOptions, CancellationToken) → Task<ItemUpdateType>` reçoit `itemResult.Item` = l'instance rafraîchie, **avant** `AfterMetadataRefresh`. `ItemUpdateType` : `None = 1`, `MetadataImport = 2`, `ImageUpdate = 4`, `MetadataDownload = 8`, `MetadataEdit = 16`.
- `IForcedProvider` (marqueur, comme `Emby.Providers.Books.BookProvider`) : le provider s'exécute sans être coché dans les réglages de la bibliothèque.
- `BookSortNameProvider` (`Name = "BookMeta Sort Name"`) relit l'OPF (`OpfLocator`), recalcule le tri (`BookMapper.BuildSortName`) puis `SortNameLock.Apply(item, tri)` : ajoute `MetadataFields.SortName` aux `LockedFields` et pose `SortName`. Retourne `MetadataEdit` si l'état a changé, sinon `None`. Journalise en Debug le tri, `InternalId`, l'identité d'instance (`RuntimeHelpers.GetHashCode`) et l'état du verrou.
- **À re-vérifier sur emby2** : `SortName` / `LockedFields` de l'item après `FullRefresh` (API), et le log Debug « tri verrouillé pour … (item #…, instance …) ». Si `LockedFields` reste vide : l'instance de `FetchAsync` n'est pas non plus celle sauvegardée ; repli = événement `ILibraryManager.ItemUpdated`.

## 10. D2-bis — `BookMeta Embedded Cover` invisible dans les réglages

`Libraries/AvailableOptions` → `ProviderManager.GetLibraryOptionsInfo` → `GetAllMetadataPlugins` → `GetPluginSummary<T>` : pour chaque type représentatif (`Book` pour le contenu « books ») il **instancie un item factice** (`Activator.CreateInstance<T>`), lui donne `Path = <InternalMetadataPath>/dummy` (**sans extension**), puis appelle `GetImageProviders(dummy, new LibraryOptions(), …, ignoreLibraryOptions)` et `AddImagePlugins` (providers `IRemoteImageProvider` et `IDynamicImageProvider`, via `GetSupportedImages(dummy)`).
→ `Supports` doit être vrai pour un `Book` dont le chemin est vide **ou sans extension** (le cycle 3 ne couvrait que « sans chemin »). `BookEmbeddedCoverProvider.Supports` : chemin vide → vrai ; extension vide → vrai ; `.cbz` → vrai ; `.epub` → option ; autre → faux.
- Case cochée par défaut : `LibraryOptionInfo.DefaultEnabled = ProviderManager.IsImageFetcherEnabledByDefault(nom, type, contenu)`, liste figée de noms Emby (§8). Pas de moyen côté provider ; **ne pas modifier la configuration des bibliothèques automatiquement**. À constater en recette : `DefaultEnabled` renvoyé par `AvailableOptions` pour « BookMeta Embedded Cover » (probablement `false` : l'utilisateur coche la case dans l'UI).

## 11. Activation automatique du récupérateur de couvertures (décision utilisateur, D2-bis)

Emby ne coche par défaut que ses propres fournisseurs (`IsImageFetcherEnabledByDefault`, liste figée de noms) : « BookMeta Embedded Cover » est proposé mais décoché. Décision utilisateur : l'activer automatiquement.

- **Point d'entrée** : `MediaBrowser.Controller.Plugins.IServerEntryPoint : IDisposable` (`void Run()`), découvert et instancié par Emby avec injection par constructeur → `BookLibraryEntryPoint(ILibraryManager, ILogManager)`.
- **Lecture** : `ILibraryManager.GetVirtualFolders() → List<VirtualFolderInfo>` (`Name`, `ItemId`, `CollectionType`, `LibraryOptions`) ; retenues : `LibraryOptions.ContentType` ou `CollectionType` = `books`.
- **Écriture** : `CollectionFolder.UpdateLibraryOptions(LibraryOptions)` (instance, obtenue par `ILibraryManager.GetItemById(long|Guid)`), qui appelle `SaveLibraryOptions` (cache + sérialisation dans l'extradata de l'item) puis déclenche `LibraryOptionsUpdated`. Repli : `CollectionFolder.SaveLibraryOptions(long, LibraryOptions)` (statique).
- **Modification** (sur une **copie** des options : `LibraryOptions` n'a pas de `Clone`, aller-retour `IJsonSerializer.SerializeToString/DeserializeFromString<LibraryOptions>` comme `CollectionFolder.SaveLibraryOptions` ; l'instance en cache du serveur n'est modifiée que par `UpdateLibraryOptions`) : le nom est ajouté à `ImageFetchers` et à `ImageFetcherOrder` de `TypeOptions[Type=="Book"]` ; rien d'autre n'est modifié. Aucun refresh déclenché.
- **Entrée `Book` absente** : **jamais d'entrée vide** (une entrée dont `ImageFetchers`/`MetadataFetchers` sont vides désactiverait les fournisseurs par défaut d'Emby : `BaseItem.IsImageFetcherEnabled` n'accepte que les noms listés). On la crée en copiant celle des réglages par défaut d'Emby, `IProviderManager.GetDefaultLibraryOptions("books")` → `TypeOptions[Book]` (mêmes `DefaultEnabled` que l'UI : `GetDefaultLibraryOptions` filtre `LibraryTypeOptions.ImageFetchers/MetadataFetchers` sur `DefaultEnabled`), puis on y ajoute BookMeta. Si ces réglages sont indisponibles : aucune entrée créée, Info « activer à la main », bibliothèque non mémorisée.
- **Lecteur désactivé** : `BookSortNameProvider` (forcé) ne fait rien si « BookMeta » figure dans `LibraryOptions.DisabledLocalMetadataReaders`.
- **Démarrage** : liste de bibliothèques vide → Debug « aucune bibliothèque de livres trouvée » (médiathèque peut-être pas encore chargée).
- **Une seule fois par bibliothèque** : `PluginConfiguration.AutoEnabledLibraryIds` (identifiants déjà traités, sauvegardée par `Plugin.SaveConfiguration()`). Une bibliothèque déjà configurée à la main est aussi mémorisée. Une case décochée ensuite n'est jamais recochée ; une bibliothèque créée plus tard est traitée au démarrage suivant. Échec de sauvegarde → Warn, non mémorisée (nouvel essai au démarrage suivant).
- **À re-vérifier sur emby2** : au redémarrage, log Info « BookMeta : récupérateur de couvertures activé pour la bibliothèque <nom> » ; `GET Library/VirtualFolders` → `LibraryOptions.TypeOptions[Book].ImageFetchers` contient « BookMeta Embedded Cover » ; la case est cochée dans l'UI ; `Plugins/BookMeta.xml` contient l'identifiant. Point ouvert : si `Run()` s'exécute avant la fin du chargement de la médiathèque, la liste peut être vide ou lever (Warn, rien d'enregistré, nouvel essai au démarrage suivant).
