# Index des tests

> Tests de specification ecrits par le test-writer, ranges en lots `<famille>/<theme>/<lot>/` ; statuts tenus par le CDP.
> Le chemin porte l'identite ; cet index ne porte que l'etat : une ligne par lot (chemin termine par `/`),
> plus des lignes fichier pour les exceptions (quarantaine). Lot sans ligne = `feature`.
> Convention : `context/COMMON.md` section 15. Statuts : `feature` | `regression` | `quarantaine`.
> Tags : `smoke`, `critical`, `slow`.

| Chemin | Niveau | Composant | Feature | Statut | Tags |
|--------|--------|-----------|---------|--------|------|
| tests/BookMeta.Tests/Integration/Plugin/Smoke/ | integration | plugin | LOT-1 / plugin charge : type Plugin, Id stable, Version | feature | smoke |
| tests/BookMeta.Tests/Integration/Sdk/Version/ | integration | plugin | LOT-1 / libs SDK Emby en 4.10.2.0 | feature | |
| tests/procedures/livres/smoke-qualif/ | procedure | plugin | LOT-1 / smoke QUALIF emby2 (plugin charge, visible, sans exception) | feature | smoke |
