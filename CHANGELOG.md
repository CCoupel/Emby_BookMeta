# Changelog

## [0.1.0] - en cours

### Ajouté
- Squelette du plugin (SDK Emby 4.10.2.0, .NET 6), tests xUnit, chaîne CI (#1).
- Documentation des faits SDK Emby 4.10.2 vérifiés, dans `docs/SDK_NOTES.md` (#2).
- Workflow `release.yml` déclenchable à la main (`workflow_dispatch`) : release à blanc produisant
  l'artefact `BookMeta-X.Y.Z` (DLL + SHA-256), sans tag ni Release (#3).
- Métadonnées locales des livres lues depuis `metadata.opf` (Calibre/Bindery), à défaut depuis l'OPF
  interne de l'epub : titre et tri, auteur (en Tag et en tri), série (en Tag et en tri), résumé nettoyé
  du HTML, date de publication, éditeur, sujets et identifiants (ISBN, BNF, Bindery) (#6).
- Couvertures locales : `cover.*` / `folder.*` du dossier du livre ; à défaut, couverture extraite de
  l'epub (`meta cover` OPF 2, `cover-image` OPF 3, sinon première image du manifest) ou première image
  d'un `.cbz`. Les PDF ne sont pas pris en charge (#7).

### Robustesse
- Un OPF malformé, un epub corrompu ou un champ absent ne remontent jamais d'exception à Emby : un
  `Warn` est journalisé et la recherche continue.
- Lecture XML sécurisée et bornée : OPF et `container.xml` limités à 5 Mo, image de couverture à 32 Mo.
- Résumé plafonné à 64 Ko, nettoyage HTML borné dans le temps (repli linéaire en cas de dépassement).
- Année « inconnue » (< 1000) et dates ambiguës ignorées ; index de série hors de 0 à 9999 ignoré pour le tri.

### Ajouté (LOT-3)
- Page de configuration **BookMeta** : option « Extraire la couverture depuis le fichier epub » (activée par défaut), persistée (#8).
- `README.md` complet : prérequis, installation, activation, ce que fait le plugin, configuration et limites connues (#5).

### Corrigé (recette LOT-2)
- Tri persistant : le `SortName` calculé depuis l'OPF est posé sur l'instance rafraîchie et verrouillé, il survit
  désormais aux actualisations. Un tri saisi à la main est réécrit au rafraîchissement suivant (modifier `metadata.opf`).
- Récupérateur de couvertures « BookMeta Embedded Cover » visible dans les réglages des bibliothèques de livres,
  et activé automatiquement une seule fois par bibliothèque (une case décochée ensuite n'est pas recochée).

