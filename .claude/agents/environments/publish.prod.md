# Publish PROD — adaptations projet BookMeta

> Compagnon de `publish.prod.template.md` (mode `rebuild-ci`) : **prevaut** sur le template.

- **Version** : `<Version>` du csproj (lecture par `sed`, jamais `cat`), 3 premiers champs = `X.Y.Z`. La reecrire
  au format `X.Y.Z` dans la balise et committer avant le tag (la CI verifie l'egalite tag/csproj sans rien ecrire).
- **Branche** : flux du template (`milestone/vX.Y.Z` mergee dans `main`, puis tag `vX.Y.Z`).
- **CI** : `gh run watch` sur le workflow `Release` (`.github/workflows/release.yml`), qui rejoue `scripts/ci.sh`
  (build, tests xUnit, audit) puis publie `BookMeta-X.Y.Z.dll` et `.sha256`.
- **Artefact publie** : `https://github.com/CCoupel/Emby_BookMeta/releases/tag/vX.Y.Z` — entree de `deploy.prod.md`.
- **Rollback** : celui du template, plus suppression de la Release (`gh release delete vX.Y.Z --yes`).
