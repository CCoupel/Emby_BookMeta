# Procedure de Test — Recette LOT-2 : metadonnees et couvertures des livres (emby2)

**Version** : version deployee en QUALIF (`X.Y.Z.a`)
**Date** : [date de la recette]
**Testeur** : QA (orchestrateur : Chrome + kubectl)

## Prerequis

- [ ] Environnement : QUALIF (`deployment/emby2`, namespace `media`), `/ceph/books` monte en lecture seule
- [ ] Plugin BookMeta deploye (LOT-1 valide) ; bibliotheque « Livres » (type Livres, dossier `/ceph/books`) creee
- [ ] Dans les options de la bibliotheque : « Lecteurs de metadonnees locaux » = `BookMeta` coche ;
      « Recuperateurs d'images » = `BookMeta Cover File` coche
- [ ] **Action manuelle obligatoire** : cocher `BookMeta Embedded Cover` dans « Recuperateurs d'images » de la bibliotheque « Livres »
      (UI Reglages de la bibliotheque, ou `LibraryOptions.TypeOptions[Book].ImageFetchers` via l'API). Emby n'active pas un nouveau fetcher de lui-meme
      (liste « par defaut » figee). Verifier ensuite que `GET Libraries/AvailableOptions?LibraryContentType=books` liste `BookMeta Embedded Cover`
      dans `TypeOptions[Book].ImageFetchers`, puis lancer **FullRefresh + ReplaceAllImages** (sinon aucune couverture embarquee).
- [ ] Cle API Emby d'`emby2` fournie par l'utilisateur au moment de la recette — jamais ecrite dans le depot ni dans ce fichier
- [ ] Action « Actualiser les metadonnees » → « Remplacer toutes les metadonnees » + « Remplacer les images existantes » lancee sur la bibliotheque,
      scan termine (sinon Emby sert son cache : risque du plan)
- Source de verite des valeurs attendues : le `metadata.opf` du dossier du livre, lu par `kubectl exec ... cat` (`/ceph/books/<Auteur>/<Titre> (<Annee>)/metadata.opf`).
  Requete type : `GET /Items?IncludeItemTypes=Book&Recursive=true&Fields=Overview,Tags,Genres,Studios,ProviderIds,PremiereDate,SortName,ProductionYear,ImageTags`.

Livres de reference : Stephenie Meyer — Fascination, Tentation, Hesitation, Revelation, Midnight Sun ; Freida McFadden — La psy, La locataire ;
Darko Macan — Frankenwood (.pdf + .cbz) ; Stephenie Meyer — « Nuits D'enfer Au Paradis », « Twilight Dixieme Anniversaire… » (sans metadata.opf).

## Scenarios

### Scenario 1 — Les cinq livres de Stephenie Meyer ont leurs metadonnees

**Validation** : AUTO — **Issues** : #6

**Objectif** : Verifier titre, tri, auteur (tag), resume, annee et identifiants pour Fascination, Tentation, Hesitation, Revelation, Midnight Sun.

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Lire chaque `metadata.opf` (kubectl) et l'item Emby correspondant (API) | 5 items `Book` trouves | | |
| 2 | Comparer `Name` a `dc:title` | Egaux (entites decodees) | | |
| 3 | Comparer `SortName` (et `ForcedSortName` si expose) | `"<file-as auteur> - <title_sort>"`, ex. Tentation = `Meyer, Stephenie - Tentation` (accents retires par Emby) ; jamais le titre seul calcule par Emby (« psy » pour « La psy ») | | |
| 3b | Lire `LockedFields` de chaque item (API, `Fields=LockedFields`) | Contient `SortName` (verrou pose par BookMeta ; sans lui Emby remet le tri a « calcule depuis le nom » apres l'actualisation) ; une seule occurrence | | |
| 4 | Comparer `Tags` | Contient `Stephenie Meyer` ; aucun tag `Serie :` si l'OPF n'a pas de `calibre:series` | | |
| 5 | Comparer `Overview` a `dc:description` | Meme texte, en francais, sans balises ni entites (`&#39;`, `&amp;`) | | |
| 6 | Comparer `ProductionYear` / `PremiereDate` a `dc:date` | Egaux | | |
| 7 | Comparer `ProviderIds` | `BNF` et `BINDERY` egaux aux `dc:identifier` | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 2 — Les livres de Freida McFadden sont renseignes

**Validation** : AUTO — **Issues** : #6

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Lire `metadata.opf` et l'item Emby de « La psy » et « La locataire » | 2 items `Book` | | |
| 2 | Comparer `Name`, `SortName`, `Tags` (auteur), `Overview`, `ProductionYear` | Conformes au contrat `contracts/models.md` §2 | | |
| 3 | Si l'OPF contient `dc:publisher` / `dc:subject` | `Studios` / `Genres` renseignes, sans doublon | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 3 — Livres sans metadata.opf : repli sur l'OPF interne de l'epub

**Validation** : AUTO — **Issues** : #6

**Objectif** : « Nuits D'enfer Au Paradis (2009) » et « Twilight Dixieme Anniversaire-la vie et la mort. Double edition (2015) » n'ont pas de `metadata.opf`.

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Verifier l'absence de `metadata.opf` (`kubectl exec ... ls` du dossier) | Absent | | |
| 2 | Extraire l'OPF interne de l'epub (`unzip -p` du `.epub`, via `META-INF/container.xml`) puis lire l'item Emby | Item existant | | |
| 3 | Comparer `Name` / `Tags` (auteur) / `Overview` / `ProductionYear` a l'OPF interne | Egaux aux valeurs de l'OPF interne | | |
| 4 | Si l'OPF interne n'a pas de titre exploitable | L'item garde le nom issu du fichier, aucune exception dans le log | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 4 — Frankenwood : .cbz avec couverture, .pdf sans couverture

**Validation** : AUTO — **Issues** : #6 #7

**Objectif** : `Darko Macan/Frankenwood (2026)/` contient un `.pdf`, un `.cbz` et un `metadata.opf` : deux items `Book`.

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Lister les items `Book` de ce dossier (API) | 2 items (pdf et cbz) | | |
| 2 | Comparer les metadonnees des deux items a `metadata.opf` | Les deux sont renseignes | | |
| 3 | `GET /Items/{id}/Images/Primary` de l'item `.cbz` | HTTP 200, image non vide | | |
| 4 | Comparer cette image a la premiere entree image (tri naturel) du `.cbz` (extraction par kubectl, hash) | Meme contenu (apres eventuel redimensionnement : memes dimensions d'origine) ou hash identique si servie telle quelle | | |
| 5 | `GET /Items/{id}/Images/Primary` de l'item `.pdf` | HTTP 404 / pas de tag d'image Primary (PDF hors v1) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 5 — Couvertures des epub

**Validation** : AUTO — **Issues** : #7

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Pour chaque epub de reference, determiner la couverture attendue dans l'epub (meta `cover` OPF 2 / `cover-image` OPF 3 / premiere image) | Couverture identifiee | | |
| 2 | `GET /Items/{id}/Images/Primary` de chaque item epub | HTTP 200, image non vide | | |
| 3 | Comparer a la couverture identifiee a l'etape 1 | Memes dimensions d'origine / hash | | |
| 4 | Aucun fichier `cover.*` / `folder.*` cree dans `/ceph/books` (montage en lecture seule) | `kubectl exec ... find /ceph/books -newer <ref>` vide | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 6 — Robustesse : aucune exception BookMeta sur toute la bibliotheque

**Validation** : AUTO — **Issues** : #6 #7

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Apres le scan complet, `grep -i bookmeta /config/logs/embyserver.txt` | Aucune ligne `Error` / `Exception` ; les `Warn` eventuels nomment un chemin de fichier | | |
| 2 | Compter les items `Book` de la bibliotheque | 17 items (15 epub + 1 pdf + 1 cbz) | | |
| 3 | Compter les items avec `HasMetadata` equivalent (Name issu de l'OPF) | Tous sauf cas explicitement justifies par un `Warn` | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 7 — Relecture apres modification de l'OPF (HasChanged)

**Validation** : AUTO — **Issues** : #6

> Attention : `/ceph/books` est en lecture seule dans `emby2` ; ce scenario n'est jouable que sur une copie de travail du dossier d'un livre,
> montee ailleurs, ou sera ignore (consigner « non jouable en QUALIF » — la couverture est alors assuree par `Integration/Opf/Sidecar`).

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Modifier le `dc:title` d'une copie de `metadata.opf` puis lancer un scan (sans « remplacer tout ») | Le titre est mis a jour | | |

**Verdict** : [ ] PASS  [ ] FAIL  [ ] NON JOUABLE

---

### Scenario 8 — Rendu visuel de la fiche livre

**Validation** : HUMAIN — **Issues** : #6 #7

**Objectif** : Jugement de lisibilite que les comparaisons de donnees ne couvrent pas : le repli « auteur en tag » est-il compris par un lecteur ?

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Ouvrir la fiche de Tentation dans l'UI Emby | Titre, couverture, resume, annee lisibles et coherents avec le livre | | |
| 2 | Observer l'auteur (tag « Stephenie Meyer ») | Identifiable comme auteur sans ambiguite acceptable (limite connue : `SupportsPeople=false`) | | |
| 3 | Parcourir la bibliotheque en grille | Couvertures presentes et nettes, pas de vignette manifestement fausse (page de texte, image decalee) | | |
| 4 | Verifier le tri par defaut | Les livres d'une meme serie/auteur se suivent dans un ordre acceptable | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

## Criteres de Validation

- [ ] Scenarios 1 a 6 passent
- [ ] Aucune exception BookMeta dans `embyserver.txt`
- [ ] Aucun fichier ecrit dans `/ceph/books`
- [ ] Scenario 8 valide par l'utilisateur au GATE QUALIF

## Notes QA

(observations)
