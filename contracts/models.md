# Contrat — Mapping OPF → `Book` (Emby 4.10.2)

> Référence unique pour `dev-plugin` (implémentation) et `test-writer` (tests de spécification). Toute divergence
> constatée dans les DLL réelles est documentée ici **avant** le code (règle contract-first) et tracée dans `CHANGELOG.md`.
> Version : v0.1.0 — issues #6 (métadonnées), #7 (couverture), #8 (configuration).
> Les lignes marquées **[GATE 2]** dépendent d'un choix utilisateur (questions Q1–Q2 du plan) ; la valeur indiquée est le défaut recommandé.

## 1. Sources, par priorité

| Priorité | Source | Condition |
|----------|--------|-----------|
| 1 | `metadata.opf` dans le dossier du livre (dossier = `Path.GetDirectoryName(item.Path)` ; `ContainingFolderPath` dépend d'un état statique d'Emby) | fichier présent et XML lisible |
| 2 | OPF interne de l'epub : `META-INF/container.xml` → `rootfiles/rootfile[@full-path]` | item `.epub`, archive zip lisible |
| — | aucune | `HasMetadata = false` (Emby garde le nom de fichier), log `Debug` |

Un OPF illisible (XML malformé, archive corrompue, rootfile absent) → log `Warn` avec le chemin, passage à la source
suivante ; **jamais d'exception** remontée à Emby. Un champ absent ou vide n'est pas affecté (la propriété reste `null`).

Formats OPF acceptés : OPF 2.0 (`<package version="2.0">`, `<meta name="calibre:…" content="…"/>`) et OPF 3.x
(`<meta property="…">`, `<meta refines="#id" …>`). Espaces de noms : `http://www.idpf.org/2007/opf`,
`http://purl.org/dc/elements/1.1/` ; le parseur est tolérant à l'absence de préfixe `opf:` sur les attributs.

## 2. Champs

| OPF | Propriété Emby (`Book : BaseItem`) | Règle |
|-----|-------------------------------------|-------|
| `dc:title` (1er) | `Name` | trim ; obligatoire pour `HasMetadata = true` |
| `meta calibre:title_sort` (OPF 2) / `meta property="file-as" refines=#title` (OPF 3) | `SortName` | **[GATE 2 Q1]** par défaut : `"<auteur file-as> - <title_sort>"` ; sans auteur : `title_sort` ; sans title_sort : `Name` |
| `dc:creator` avec `opf:role="aut"` ou sans rôle (OPF 3 : `meta property="role" refines` = `aut`) | `Tags` (+ `SortName` ci-dessus) | **[GATE 2 Q1]** `Book.SupportsPeople = false` dans Emby 4.10.2 : les `People` ne sont ni stockés ni affichés pour un livre → auteur exposé en **Tag** (nom affiché, un tag par auteur). `opf:file-as` sert au tri ; à défaut, dérivé `"Nom, Prénom"` depuis le dernier mot |
| `dc:description` | `Overview` | décoder les entités HTML (`&#39;`, `&amp;`, `&eacute;`…), retirer les balises, normaliser les blancs ; `<br>`/`<p>` → saut de ligne |
| `dc:date` | `PremiereDate`, `ProductionYear` | formats `yyyy-MM-dd`, `yyyy-MM`, `yyyy`, ISO 8601 complet ; date invalide → ignorée (log `Debug`) |
| `dc:language` | — | aucun champ Emby adapté sur `Book` : **non mappé** en v1 (voir plan, Q3) |
| `dc:publisher` | `Studios` (via `SetStudios`) | un studio par `dc:publisher` ; `Book.SupportsStudios = true` |
| `dc:subject` | `Genres` (via `AddGenre`) | un genre par sujet, dédoublonné (insensible à la casse) ; `Book.SupportsGenres = true` |
| `meta calibre:series` / `meta calibre:series_index` (OPF 3 : `belongs-to-collection` + `group-position`) | `Tags` + `SortName` | **[GATE 2 Q2]** Emby n'a pas de série de livres (`Book` n'implémente pas `IHasSeries`) : Tag `"Série : <nom>"` ; `SortName = "<auteur> - <série> <index 00> - <title_sort>"` quand la série est connue. `IndexNumber` **non renseigné** (affiché comme un numéro d'épisode) |
| `dc:identifier opf:scheme="ISBN"` (ou `urn:isbn:…`) | `ProviderIds["ISBN"]` | clé `ISBN` = `MetadataProviders.ISBN` (Emby fournit un lien externe) ; ISBN nettoyé (tirets retirés) |
| `dc:identifier opf:scheme="BNF"` | `ProviderIds["BNF"]` | valeur brute (`cb43546026q`) |
| `dc:identifier opf:scheme="BINDERY"` | `ProviderIds["BINDERY"]` | valeur brute |
| autres `dc:identifier` | — | ignorés |

`MetadataResult<Book>` : `HasMetadata = true` dès qu'un `dc:title` est lu ; `Item = new Book { … }` ;
`ResultLanguage = dc:language` si présent (information, sans effet d'affichage) ; `Provider = "BookMeta"`.

## 3. Relecture (`IHasItemChangeMonitor.HasChanged`)

`true` si `LastWriteTimeUtc` de `metadata.opf` (sinon du fichier livre) `> item.DateLastRefreshed`. Fichier absent → `false`.
Aucune exception : erreur d'accès → `false` + log `Warn`.

## 4. Couverture (issue #7)

| Priorité | Source | Fournisseur | Condition |
|----------|--------|-------------|-----------|
| 1 | `cover.jpg` / `cover.jpeg` / `cover.png` / `cover.webp`, puis `folder.*` (mêmes extensions), dans le dossier du livre | `ILocalImageFileProvider` → `LocalImageInfo { Type = Primary, FileInfo }` | fichier présent (comparaison insensible à la casse) |
| 2 | epub : item du manifest référencé par `meta name="cover" content="<id>"` (OPF 2), sinon item `properties~="cover-image"` (OPF 3), sinon première image (`media-type image/*`) du manifest dans l'ordre du document | `IDynamicImageProvider` → `DynamicImageResponse { Stream, Format }` | item `.epub`, option **`EnableEpubCoverExtraction = true`** (#8, défaut `true`) |
| 3 | cbz : première entrée image (`.jpg/.jpeg/.png/.webp/.gif`) par tri naturel du nom, dossiers `__MACOSX` et fichiers cachés exclus | idem | item `.cbz` |
| — | pdf, autres | — | hors v1 : `Supports(item) = false` |

`ImageFormat` déduit de l'extension/du `media-type` (`Jpg`, `Png`, `Webp`, `Gif`) ; type inconnu → pas d'image.
Archive corrompue, entrée absente, image vide → pas de couverture, log `Warn`, jamais d'exception.
Seule `ImageType.Primary` est fournie (`GetSupportedImages`).

## 5. Configuration (issue #8)

`PluginConfiguration : BasePluginConfiguration` — `bool EnableEpubCoverExtraction = true`. Persistance par Emby
(`BasePlugin<T>.Configuration`, XML dans `/config/plugins/configurations/BookMeta.xml`). Page : fragment HTML +
module AMD, lecture/écriture via `ApiClient.getPluginConfiguration(PLUGIN_ID)` / `updatePluginConfiguration` —
API standard d'Emby, aucun endpoint propre au plugin.

## 6. Noms stables (visibles dans les réglages de bibliothèque Emby)

| Élément | Valeur |
|---------|--------|
| `IMetadataProvider.Name` (lecteur local) | `BookMeta` |
| `IImageProvider.Name` (couverture dossier) | `BookMeta Cover File` |
| `IImageProvider.Name` (couverture embarquée epub/cbz) | `BookMeta Embedded Cover` |
| `Plugin.Name` | `BookMeta` |
| `Plugin.Id` | `097ed1ba-f0ae-496f-8f56-e54599fe54b1` (fixé au squelette #1), **jamais modifié ensuite** |

Ces noms sont ceux qu'un utilisateur coche/décoche dans « Lecteurs de métadonnées locaux » et « Récupérateurs
d'images » d'une bibliothèque : les changer est un changement **BREAKING** (réglages perdus).

## 7. Écarts décidés (revue LOT-2, 2026-10-31)

Précisions apportées à l'implémentation, jugées justifiées par la revue ; elles complètent les sections ci-dessus.

| Sujet | Règle retenue |
|-------|---------------|
| Dates | Année < 1000 ignorée (valeur « inconnue » de Calibre) ; année lue dans le décalage d'origine ; formats ambigus (`1/2/2020`) refusés ; `dc:date` d'`opf:event` `creation` / `modification` ignoré |
| OPF sans titre | Écarté avec `Warn` ; la recherche continue (sidecar → OPF interne de l'epub) ; `HasMetadata = false` si aucun OPF titré |
| Dossier du livre | Dérivé de `item.Path` (jamais `ContainingFolderPath`) |
| Couverture epub | Candidats dans l'ordre `meta cover` → `cover-image` → images du manifest ; un candidat dont l'entrée est absente ou vide laisse passer le suivant ; `Warn` si des candidats existaient sans résultat |
| Série sans auteur | `SortName = "<Série> NN - <titre de tri>"` |
| Index de série | Borné à `0 ≤ i ≤ 9999` ; hors bornes, ignoré dans le tri (le Tag « Série : X » est conservé) |
| « Pas d'image » | Réponse `DynamicImageResponse` **vide** (ni `Stream` ni `Path`), jamais `null` — *à confirmer en recette emby2* |
| Description | Plafonnée à 64 Ko avant nettoyage ; nettoyage borné par un délai (1 s), repli linéaire + `Warn` |
| Tailles | OPF et `container.xml` ≤ 5 Mo ; image de couverture ≤ 32 Mo |
| Tri persistant | Le `SortName` calculé est posé sur l'item réel **et le champ `SortName` est verrouillé** (Emby le réinitialise sinon à chaque actualisation) ; un tri saisi à la main est réécrit au refresh suivant |
| Récupérateur d'images embarqué | `Supports` vrai pour un livre sans chemin (liste des réglages) ; l'utilisateur doit cocher « BookMeta Embedded Cover » dans la bibliothèque |
