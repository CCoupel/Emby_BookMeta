# Prompt de démarrage — Claude Code

## Contexte

Tu développes **BookMeta**, un plugin C# pour Emby Server 4.10.2 (Linux, conteneur `emby/embyserver`).

Problème : Emby gère mal les **livres** (bibliothèque de type Livres) : titre = nom de fichier, ni résumé,
ni auteur trié, ni série, couverture aléatoire. Le plugin officiel Open Library ne trouve presque rien
en français. Les livres audio sont bien gérés : **hors périmètre**.

Les livres sont importés par **Bindery** (gestionnaire type *arr) qui écrit, à côté de chaque livre,
un `metadata.opf` au format Calibre (OPF 2.0) rempli depuis la BnF. Le plugin doit lire ces fichiers
localement : **aucun appel réseau**.

Arborescence (bibliothèque Emby « Livres » = `/ceph/books`) :
```
/ceph/books/<Auteur>/<Titre> (<Année>)/<Titre> - <Auteur>.epub   (ou .pdf, .cbz)
/ceph/books/<Auteur>/<Titre> (<Année>)/metadata.opf
```
Certains dossiers n'ont pas de `metadata.opf` (anciens imports) : utiliser alors l'OPF interne de l'epub.

Exemple réel de `metadata.opf` écrit par Bindery (à mettre en fixture de test) :
```xml
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://www.idpf.org/2007/opf" unique-identifier="bindery-id" version="2.0">
  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:opf="http://www.idpf.org/2007/opf">
    <dc:title>Tentation</dc:title>
    <meta name="calibre:title_sort" content="Tentation"/>
    <dc:creator opf:role="aut" opf:file-as="Meyer, Stephenie">Stephenie Meyer</dc:creator>
    <dc:identifier id="bindery-id" opf:scheme="BINDERY">84</dc:identifier>
    <dc:identifier opf:scheme="BNF">cb43546026q</dc:identifier>
    <dc:language>fr</dc:language>
    <dc:date>2006-01-01</dc:date>
    <dc:description>Rejetée par celui qu elle aime passionnément, Bella ne s en relève pas…</dc:description>
  </metadata>
</package>
```
Bindery peut aussi écrire `<meta name="calibre:series" .../>` et `<meta name="calibre:series_index" .../>`
quand le livre est rattaché à une série. Les descriptions peuvent contenir des entités HTML (`&#39;`).

## Référence

- Reprendre la structure, le `.csproj` (net6.0, SDK Emby via `libs/*.dll` en `<Private>false</Private>`),
  le `TEMPLATE_claude`, le processus `build/` (candidate/qualif/prod) et les conventions de
  `../Emby_Badges` (projet du même auteur, déjà en production sur ce serveur).
- Modèle fonctionnel (ne pas copier aveuglément, GPL-3) : plugin Jellyfin **Bookshelf**
  (`jellyfin/jellyfin-plugin-bookshelf`), qui lit l'OPF et extrait la couverture des epub.
- Le SDK Emby est peu documenté : vérifier chaque interface/type dans `libs/MediaBrowser.*.dll`
  (réflexion / décompilation) plutôt que de supposer l'API Jellyfin.

## Périmètre fonctionnel (v1)

1. **Métadonnées locales** — `ILocalMetadataProvider<Book>` (+ `IHasItemChangeMonitor` pour relire
   quand l'OPF change). Source : `metadata.opf` du dossier du livre ; sinon OPF interne de l'epub
   (`META-INF/container.xml` → rootfile). Champs :
   - `dc:title` → Name ; `calibre:title_sort` → SortName
   - `dc:creator` (role `aut` ou sans rôle) → People (type auteur adapté dans Emby ; vérifier le
     `PersonType` réellement affiché par Emby pour les livres)
   - `dc:description` → Overview (décoder les entités HTML, retirer les balises)
   - `dc:date` → PremiereDate + ProductionYear ; `dc:language` → langue si le modèle le permet
   - `dc:publisher` → Studios ; `dc:subject` → Genres/Tags
   - `calibre:series` / `calibre:series_index` → série + IndexNumber (vérifier comment Emby 4.10
     modélise une série de livres ; à défaut, Tags + tri)
   - identifiants ISBN / BNF / BINDERY → ProviderIds
2. **Couverture** — fournisseur d'image locale : `cover.jpg|jpeg|png` ou `folder.*` dans le dossier ;
   sinon couverture extraite de l'epub (meta `cover` OPF 2, `properties="cover-image"` OPF 3,
   à défaut première image du manifest). CBZ : première image de l'archive. PDF : hors v1.
3. Robustesse : OPF malformé, epub corrompu, champs absents → jamais d'exception remontée à Emby,
   log `Warn` et on continue. Aucun fichier écrit dans la bibliothèque (montée en lecture seule).
4. Page de config minimale (facultative) : activer/désactiver l'extraction de couverture epub.

## Tests

- Tests unitaires xunit : parsing OPF (fixture ci-dessus, OPF 3, série, entités HTML, OPF invalide),
  extraction de couverture depuis un epub de test généré dans les tests.
- Recette sur le serveur réel (cluster Kubernetes, voir ci-dessous) avec les livres existants :
  Fascination, Tentation, Hésitation, Révélation, Midnight Sun (Stephenie Meyer) ;
  La psy, La locataire (Freida McFadden) ; Frankenwood (Darko Macan, .cbz + .pdf, sans epub).
  Critère : titre, auteur, résumé français, année et couverture corrects dans Emby après
  « Actualiser les métadonnées » de la bibliothèque Livres.

## Déploiement de test (Emby dans Kubernetes)

- `export KUBECONFIG=/mnt/c/Users/cyril/Documents/PROJETS/INFRA/Infra-K8S/.config/kubeconfig`
  (ne jamais afficher ce fichier).
- Emby = `deploy/emby`, namespace `media`, plugins dans `/config/plugins`.
- Installer : `kubectl -n media cp BookMeta.dll <pod-emby>:/config/plugins/BookMeta.dll`
  puis `kubectl -n media rollout restart deploy/emby`. Désinstaller = supprimer la DLL + restart.
- Logs : `/config/logs/embyserver.txt` dans le pod.
- Vérification par API Emby (`https://coupel.net/emby`, clé API créée par l'utilisateur dans
  Tableau de bord → Clés API ; ne jamais l'écrire dans le dépôt).
- Pas de SDK .NET dans ce WSL : utiliser la chaîne de build des autres projets ou un conteneur
  `mcr.microsoft.com/dotnet/sdk:6.0`.

## Tâche initiale

1. Lire `../Emby_Badges` (CLAUDE.md, csproj, build/) et initialiser ce projet sur le même modèle
   (CLAUDE.md, README, CHANGELOG, .gitignore, `libs/` avec les DLL SDK Emby 4.10.2).
2. Explorer le SDK dans `libs/` : interfaces de providers locaux et d'images pour `Book`,
   `PersonType`, modélisation des séries de livres. Consigner les conclusions dans `docs/SDK_NOTES.md`.
3. Proposer l'architecture (fichiers, classes) et me la faire valider avant d'écrire le code.
