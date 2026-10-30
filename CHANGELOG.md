# Changelog

## [0.1.0] - en cours

### Ajouté
- Squelette du plugin (SDK Emby 4.10.2.0, .NET 6), tests xUnit, chaîne CI (#1).
- Documentation des faits SDK Emby 4.10.2 vérifiés, dans `docs/SDK_NOTES.md` (#2).
- Workflow `release.yml` déclenchable à la main (`workflow_dispatch`) : release à blanc produisant
  l'artefact `BookMeta-X.Y.Z` (DLL + SHA-256), sans tag ni Release (#3).
