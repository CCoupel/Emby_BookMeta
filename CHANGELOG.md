# Changelog

Toutes les évolutions notables de BookMeta sont consignées dans ce fichier.

Le format s'appuie sur [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
et le projet suit le [Versionnement sémantique](https://semver.org/lang/fr/).

## [1.0.0] - 2026-10-31

Première version officielle de BookMeta. Le code est identique à la version 0.1.0.

BookMeta renseigne la bibliothèque **Livres** d'Emby Server 4.10.2 à partir des fichiers locaux, sans aucun
appel réseau.

### Fonctionnalités

- **Métadonnées locales** lues depuis `metadata.opf` (Calibre / Bindery), à défaut depuis l'OPF interne de
  l'epub : titre et tri, auteur (en tag et en tri), série (en tag et en tri), résumé nettoyé, date de
  publication, éditeur, sujets (genres) et identifiants (ISBN, BNF, Bindery).
- **Couvertures locales** : `cover.*` / `folder.*` du dossier ; à défaut, couverture de l'epub ou première image
  d'un `.cbz`. Les PDF ne sont pas pris en charge.
- **Récupérateur « BookMeta Embedded Cover »** (epub et cbz), activé automatiquement une fois par bibliothèque de livres.
- **Tri persistant** : le tri calculé depuis l'OPF survit aux actualisations des métadonnées.
- **Page de configuration** : option « Extraire la couverture depuis le fichier epub ».
- **Robustesse** : un fichier malformé ou un champ absent ne provoque jamais d'erreur dans Emby ; un avertissement
  est journalisé et la recherche continue.

### Limites connues

- Les auteurs apparaissent en tags (onglet **Mot-clé**) : Emby 4.10 n'affiche pas les personnes pour un livre.
- Les séries sont en tag `Série : <nom>` et en tri ; le numéro de tome n'est pas affiché.
- Un tri saisi à la main est réécrit à chaque actualisation : le modifier dans `metadata.opf`.
- Le tri reste verrouillé après désinstallation (procédure de retour dans le README).
- Pas de `.nfo` : `metadata.opf` est la seule source de vérité.

Le détail des versions précédentes figure dans la section [0.1.0] ci-dessous.

## [0.1.0] - 2026-10-31

Première version de BookMeta : renseigner la bibliothèque **Livres** d'Emby Server 4.10.2 à partir des
fichiers locaux, sans aucun appel réseau.

### Ajouté

- **Métadonnées locales** lues depuis `metadata.opf` (Calibre / Bindery), à défaut depuis l'OPF interne
  de l'epub : titre et tri, auteur (en tag et en tri), série (en tag et en tri), résumé nettoyé du HTML,
  date de publication, éditeur, sujets (genres) et identifiants (ISBN, BNF, Bindery).
- **Couvertures locales** : `cover.*` / `folder.*` du dossier du livre ; à défaut, couverture déclarée dans
  l'epub (OPF 2 `meta cover`, OPF 3 `cover-image`, sinon première image) ou première image d'un `.cbz`.
  Les PDF ne sont pas pris en charge.
- **Récupérateur de couvertures « BookMeta Embedded Cover »** (epub et cbz), activé automatiquement une
  seule fois par bibliothèque de livres. Une case décochée ensuite n'est jamais recochée.
- **Tri persistant** : le tri calculé depuis l'OPF est verrouillé et survit aux actualisations des métadonnées.
- **Page de configuration** : option « Extraire la couverture depuis le fichier epub » (activée par défaut).
- **Documentation** : `README.md` avec installation, activation, configuration et limites connues.

### Robustesse

- Un OPF malformé, un epub corrompu ou un champ absent ne remontent jamais d'exception à Emby : un
  avertissement est journalisé et la recherche continue.
- Lecture XML sécurisée et bornée : OPF et `container.xml` limités à 5 Mo, image de couverture à 32 Mo.
- Résumé plafonné à 64 Ko, nettoyage HTML borné dans le temps avec repli linéaire en cas de dépassement.
- Année « inconnue » (antérieure à 1000) et dates ambiguës ignorées ; index de série hors de 0 à 9999 ignoré pour le tri.

### Limites connues

- Les auteurs apparaissent en tags (onglet **Mot-clé**) : Emby 4.10 n'affiche pas les personnes pour un livre.
- Les séries sont en tag `Série : <nom>` et en tri ; le numéro de tome n'est pas affiché.
- Un tri saisi à la main est réécrit à chaque actualisation : le modifier dans `metadata.opf`.
- Le tri reste verrouillé après désinstallation (procédure de retour dans le README).
- Compatibilité testée sur Emby Server 4.10.2 uniquement.
