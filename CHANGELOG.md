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
