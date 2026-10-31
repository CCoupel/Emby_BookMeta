# BookMeta

Plugin **Emby Server 4.10.2** qui renseigne la bibliothèque **Livres** à partir de fichiers locaux :
le `metadata.opf` du dossier du livre (écrit par Bindery depuis la BnF), à défaut l'OPF interne de l'epub,
et les couvertures du dossier, de l'epub ou du `.cbz`.

**Aucun appel réseau** : le plugin ne consulte aucun service externe. Il ne modifie jamais les fichiers de la
bibliothèque et n'écrit aucun `.nfo`.

> Version en développement (v0.1.0). Le plugin n'est pas encore publié dans une release.

---

## Prérequis

- Emby Server **4.10.2** (testé uniquement sur cette version, image Docker officielle `emby/embyserver`).
- Une bibliothèque de type **Livres** dont le dossier contient les livres, organisés ainsi :

  ```
  /ceph/books/<Auteur>/<Titre> (<Année>)/
      metadata.opf      ← écrit par Bindery (optionnel)
      cover.jpg         ← couverture (optionnel)
      <Titre>.epub      ← ou .cbz
  ```

Un fichier = un item : un livre fourni en deux formats (ex. `.pdf` + `.cbz`) apparaît en deux items.

---

## Installation

1. Télécharger l'asset `BookMeta-X.Y.Z.dll` et son `BookMeta-X.Y.Z.dll.sha256` depuis la release GitHub
   `vX.Y.Z` (ou, en local, `bash scripts/ci.sh` produit `dist/BookMeta.dll`).
2. Vérifier l'intégrité **avant** de renommer le fichier :

   ```bash
   sha256sum -c BookMeta-X.Y.Z.dll.sha256
   ```

3. Copier la DLL **sous le nom `BookMeta.dll`** dans le dossier `plugins` du répertoire de données d'Emby :
   - image Docker officielle : `/config/plugins/` ;
   - hors conteneur : `<dossier de données Emby>/plugins/`.

   Exemples (à adapter, ce ne sont pas les seules méthodes) :

   ```bash
   docker cp BookMeta-X.Y.Z.dll <conteneur>:/config/plugins/BookMeta.dll
   kubectl cp BookMeta-X.Y.Z.dll <namespace>/<pod>:/config/plugins/BookMeta.dll
   ```

4. Redémarrer Emby (`docker restart <conteneur>` ou `kubectl rollout restart`).
5. Vérifier dans **Tableau de bord → Plugins** que **BookMeta** apparaît, sans erreur.

### Mise à jour

Remplacer `BookMeta.dll` par la nouvelle DLL (même procédure, même nom de fichier), puis redémarrer Emby.
Recharger ensuite la page de configuration sans cache (Ctrl+Maj+R) : le navigateur peut afficher l'ancienne.

### Désinstallation

1. Supprimer `plugins/BookMeta.dll`, puis redémarrer Emby.
2. Optionnel : supprimer `plugins/configurations/BookMeta.xml` (réglages du plugin).

Les métadonnées déjà écrites restent dans Emby. **Le tri verrouillé reste figé** après désinstallation :
voir [Limites connues](#limites-connues) pour la procédure de retour.

---

## Activation

Les réglages se trouvent dans **Médiathèque** → carte de la bibliothèque **Livres** (réglages de la bibliothèque).

### Lecteur de métadonnées

Section **« Lecteurs de metadonnées »** → cocher **BookMeta**. Il lit `metadata.opf` puis, à défaut,
l'OPF interne de l'epub.

### Récupérateur de couvertures — activation automatique

Section **« Book chercheurs d'image »** → **BookMeta Embedded Cover** (couvertures de l'epub et du cbz).

Ce récupérateur est proposé par Emby **décoché** : le plugin l'active automatiquement pour chaque bibliothèque de
livres, **une seule fois** au démarrage.

- Il est ajouté aux récupérateurs d'images de la bibliothèque ; aucun autre réglage n'est modifié.
- Si vous le décochez ensuite, il reste décoché : le plugin ne le recoche jamais.
- Une bibliothèque créée après l'installation est traitée au démarrage suivant d'Emby.
- Pour l'activer à la main : cocher **BookMeta Embedded Cover** dans la section ci-dessus.

### Option à laisser désactivée

**« Enregistrer les images dans les dossiers multimédia »** : BookMeta n'écrit rien dans la bibliothèque ;
laisser cette option décochée.

Après installation ou changement de réglages, lancer **Actualiser les métadonnées → Remplacer toutes les
métadonnées et images** sur la bibliothèque pour appliquer le plugin aux livres déjà présents.

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
| Sujets | Genres (voir [Limites connues](#limites-connues)) |
| ISBN, BNF, Bindery | Identifiants externes |

### Couvertures

Dans l'ordre :

1. `cover.*` ou `folder.*` (jpg, jpeg, png, webp) dans le dossier du livre ;
2. couverture de l'epub (`meta cover` OPF 2, `cover-image` OPF 3, sinon première image du manifest) ;
3. première image d'un `.cbz`.

Les PDF ne sont pas pris en charge en v0.1.0.

### Journal

Les avertissements du plugin sont écrits dans `logs/embyserver.txt` (ou `logs/embyserver-<n>.txt` pour les
journaux tournés : prendre le plus récent). Dans l'image Docker : `/config/logs/`.

### Robustesse

Un fichier malformé, un epub corrompu ou un champ absent ne provoque jamais d'erreur dans Emby : le plugin
journalise un avertissement et passe à la source suivante.

---

## Configuration

Menu **BookMeta** (Emby Server) : case **« Extraire la couverture depuis le fichier epub »**, puis **Enregistrer**.

| Option | Défaut | Effet |
|--------|--------|-------|
| Extraire la couverture depuis le fichier epub | activée | Désactivée : aucune nouvelle extraction depuis l'epub (les `cover.*` du dossier restent prises en compte). |

Désactiver l'option **n'efface pas** les couvertures déjà présentes : il faut supprimer l'image du livre puis
actualiser ses métadonnées pour la retirer.

---

## Limites connues

- **Auteurs en tag, pas en personnes.** Emby 4.10 n'affiche pas les personnes (auteurs) pour un livre ; ils
  apparaissent comme tags, consultables dans l'onglet **Mot-clé**. Les onglets de la bibliothèque sont
  **Livres**, **Mot-clé** et **Dossiers** : pas d'onglet Auteurs ni Genres pour les livres.
- **Genres sans onglet.** Les sujets OPF sont renseignés dans le champ Genres d'Emby, mais n'ont pas d'onglet
  dédié (filtrables par le filtre de la bibliothèque si disponible). Décision : pas de tags « Genre : ».
- **Séries en tag et en tri.** Emby n'a pas de notion de série pour les livres : la série est un tag
  `Série : <nom>` et sert au tri (`Auteur - Série NN - Titre`). Le numéro de tome n'est pas affiché.
- **Tri verrouillé.** BookMeta verrouille le champ **Tri** des livres pour que le tri calculé depuis l'OPF
  survive aux actualisations. Conséquences :
  - un tri saisi à la main est réécrit à chaque actualisation : le modifier dans `metadata.opf` à la place ;
  - le verrou **reste après désinstallation** (le tri reste figé). Procédure de retour : déverrouiller le champ
    **Tri** de l'élément, puis actualiser ses métadonnées.
- **« Remplacer toutes les métadonnées »** écrase les modifications faites à la main dans Emby.
- **PDF** : pas de couverture extraite.
- **Livres audio** : hors périmètre.
- **Pas de `.nfo`.** `metadata.opf` est la seule source de vérité ; aucun export NFO, ni par le plugin ni par le
  saver NFO d'Emby.
- **Lecture seule.** Le plugin ne crée, ne modifie et ne supprime aucun fichier de la bibliothèque.
- **Compatibilité** : testée sur Emby Server 4.10.2 uniquement.

---

## Licence

Aucune licence. Dépôt public, tous droits réservés.

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
