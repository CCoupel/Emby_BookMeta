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
| tests/BookMeta.Tests/Integration/Opf/Sidecar/ | integration | opf | LOT-2 / metadata.opf Bindery dans le dossier (Tentation), priorites, HasChanged | feature | critical |
| tests/BookMeta.Tests/Integration/Opf/Epub/ | integration | opf | LOT-2 / repli sur l'OPF interne de l'epub | feature | |
| tests/BookMeta.Tests/Integration/Opf/Robustesse/ | integration | opf | LOT-2 / OPF, epub corrompus, XXE : Warn sans exception | feature | |
| tests/BookMeta.Tests/Integration/Mapping/Book/ | integration | providers | LOT-2 / mapping OPF → Book (tags, tri, serie, resume, dates, ids) | feature | |
| tests/BookMeta.Tests/Integration/Covers/Folder/ | integration | covers | LOT-2 / cover.* et folder.* du dossier | feature | |
| tests/BookMeta.Tests/Integration/Covers/Epub/ | integration | covers | LOT-2 / couverture extraite de l'epub (OPF 2, OPF 3, premiere image) | feature | |
| tests/BookMeta.Tests/Integration/Covers/Cbz/ | integration | covers | LOT-2 / premiere image du cbz (tri naturel) | feature | |
| tests/procedures/livres/recette-livres/ | procedure | providers | LOT-2 / recette emby2 sur les livres de reference | feature | |
| tests/BookMeta.Tests/Integration/Mapping/Tri/ | integration | providers | LOT-2 / régression D1 : tri verrouillé sur l'item réel (SortName ∈ LockedFields) | regression | |
| tests/BookMeta.Tests/Integration/Configuration/Toggle/ | integration | configuration | LOT-3 / option EnableEpubCoverExtraction : défaut, persistance XML, AutoEnabledLibraryIds, effet sur les récupérateurs | feature | |
| tests/procedures/livres/config-page/ | procedure | configuration | LOT-3 / page de configuration BookMeta (case epub, persistance après redémarrage) | feature | |
