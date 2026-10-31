# BookMeta

Plugin **Emby Server 4.10.2** qui renseigne la bibliothèque **Livres** à partir de fichiers locaux :
le `metadata.opf` du dossier du livre (écrit par Bindery depuis la BnF), à défaut l'OPF interne de l'epub,
et les couvertures du dossier, de l'epub ou du `.cbz`.

**Aucun appel réseau** : le plugin ne consulte aucun service externe. Il ne modifie jamais les fichiers de la
bibliothèque (montée en lecture seule) et n'écrit aucun `.nfo`.

> Version en développement (v0.1.0). Le plugin n'est pas encore publié dans une release.

---

## Prérequis

- Emby Server **4.10.2** (testé sur le conteneur `emby/embyserver`).
- Une bibliothèque de type **Livres** (*Books*) dont le dossier contient les livres, organisés ainsi :

  ```
  /ceph/books/<Auteur>/<Titre> (<Année>)/
      metadata.opf      ← écrit par Bindery (optionnel)
      cover.jpg         ← couverture (optionnel)
      <Titre>.epub      ← ou .cbz
  ```

---

## Installation

1. Récupérer `BookMeta.dll` (asset de la release GitHub `vX.Y.Z`, ou build local : `bash scripts/ci.sh`
   produit `dist/BookMeta.dll`).
2. Copier la DLL dans le dossier des plugins d'Emby, `/config/plugins/` (sur le conteneur Emby :
   `/config/plugins/BookMeta.dll`).

   ```bash
   kubectl cp BookMeta.dll <namespace>/<pod>:/config/plugins/BookMeta.dll
   ```

3. Redémarrer Emby (`kubectl rollout restart` ou redémarrage du conteneur).
4. Vérifier dans **Tableau de bord → Plugins** que **BookMeta** apparaît, sans erreur.

---

## Activation

Les réglages se trouvent dans **Réglages de la bibliothèque Livres** (sections *Lecteurs de métadonnées locaux* et *Récupérateurs d'images*).

### Lecteur de métadonnées

- **BookMeta** (lecteur de métadonnées locales) : à cocher dans *Lecteurs de métadonnées locaux* si ce n'est pas déjà fait.
  Il lit `metadata.opf` puis, à défaut, l'OPF interne de l'epub.

### Récupérateur de couvertures — activation automatique

Le récupérateur **BookMeta Embedded Cover** (couvertures de l'epub et du cbz) est proposé par Emby **décoché** :
le plugin l'active automatiquement pour chaque bibliothèque de livres, **une seule fois** au démarrage.

- Il est ajouté aux récupérateurs d'images de la bibliothèque ; aucun autre réglage n'est modifié.
- Si vous le décochez ensuite, il reste décoché : le plugin ne le recoche jamais.
- Une bibliothèque créée après l'installation est traitée au démarrage suivant d'Emby.
- Pour l'activer à la main : section *Récupérateurs d'images* → cocher **BookMeta Embedded Cover**.

Après installation, lancer **Actualiser les métadonnées → Remplacer toutes les métadonnées et images**
sur la bibliothèque pour appliquer le plugin aux livres déjà présents.

---

## Ce que fait le plugin

### Métadonnées

Source, par ordre de priorité :

1. `metadata.opf` dans le dossier du livre ;
2. à défaut, l'OPF interne de l'epub (`META-INF/container.xml` → rootfile).

Champs renseignés :

| Information | Où elle apparaît dans Emby |
|-------------|----------------------------|
| Titre | Nom |
| Titre de tri, auteur, série | Tri (`SortName`) |
| Auteur | Tag (un par auteur) |
| Série | Tag `Série : <nom>` |
| Résumé (HTML nettoyé) | Résumé |
| Date de publication | Date de première diffusion et année |
| Éditeur | Studios |
| Sujets | Genres |
| ISBN, BNF, Bindery | Identifiants externes |

### Couvertures

Dans l'ordre :

1. `cover.*` ou `folder.*` (jpg, jpeg, png, webp) dans le dossier du livre ;
2. couverture de l'epub (`meta cover` OPF 2, `cover-image` OPF 3, sinon première image du manifest) ;
3. première image d'un `.cbz`.

Les PDF ne sont pas pris en charge en v0.1.0.

### Robustesse

Un fichier malformé, un epub corrompu ou un champ absent ne provoque jamais d'erreur dans Emby : le plugin
journalise un avertissement dans `/config/logs/embyserver.txt` et passe à la source suivante.

---

## Configuration

Page **BookMeta** dans **Tableau de bord → Plugins** :

| Option | Défaut | Effet |
|--------|--------|-------|
| Extraire la couverture depuis le fichier epub | activée | Si désactivée, la couverture déclarée dans l'epub n'est pas utilisée (les `cover.*` du dossier restent prises en compte). |

---

## Limites connues

- **Auteurs en tag, pas en personnes.** Emby 4.10 n'affiche pas les personnes (auteurs) pour un livre ; ils
  apparaissent comme tags. Il n'y a donc pas d'onglet **Auteurs** ni **Genres** pour les livres : utiliser
  l'onglet **Mot-clé** (tags).
- **Séries en tag et en tri.** Emby n'a pas de notion de série pour les livres : la série est un tag
  `Série : <nom>` et sert au tri (`Auteur - Série NN - Titre`). Le numéro de tome n'est pas affiché.
- **Tri verrouillé.** BookMeta verrouille le champ **Tri** des livres pour que le tri calculé depuis l'OPF
  survive aux actualisations. Conséquences :
  - un tri saisi à la main est réécrit à chaque actualisation : le modifier dans `metadata.opf` à la place ;
  - le verrou **reste après désinstallation** (le tri reste figé). Pour revenir au tri Emby : déverrouiller le
    champ **Tri** de l'élément puis actualiser ses métadonnées.
- **PDF** : pas de couverture extraite.
- **Livres audio** : hors périmètre.
- **Pas de `.nfo`.** `metadata.opf` est la seule source de vérité ; aucun export NFO, ni par le plugin ni par le
  saver NFO d'Emby.
- **Lecture seule.** Le plugin ne crée, ne modifie et ne supprime aucun fichier de la bibliothèque.

---

## Développement

```bash
bash scripts/ci.sh        # build Release → dist/BookMeta.dll, tests xUnit, audit des paquets
```

Ou, sans le script : `dotnet build src/BookMeta/BookMeta.csproj -c Release -o dist/` puis
`dotnet test tests/BookMeta.Tests/`.

Le SDK Emby 4.10.2 est référencé depuis `libs/MediaBrowser.*.dll` (committé volontairement).

Documentation :

- [`docs/CADRAGE.md`](docs/CADRAGE.md) — cadrage, périmètre, décisions ;
- [`docs/SDK_NOTES.md`](docs/SDK_NOTES.md) — faits vérifiés sur le SDK Emby 4.10.2 ;
- [`CHANGELOG.md`](CHANGELOG.md) — historique des versions.
