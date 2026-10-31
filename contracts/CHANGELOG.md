# Changelog des contrats

> Changements de contrats (API, schemas) : `BREAKING` | `CHANGED` | `ADDED`. Lu par le CDP au GATE 2.

## [20261031-b] — v0.1.0 : défauts de recette emby2 (cycle 3)

- **[ADDED]** `contracts/models.md` §7 (« Écarts décidés ») : lignes « Tri persistant » (`SortName` verrouillé sur l'item réel, effacé sinon par
  `AfterMetadataRefresh`) et « Récupérateur d'images embarqué » (`Supports` vrai pour un livre sans chemin ; activation manuelle de
  « BookMeta Embedded Cover »). Les §2 et §4 sont inchangés. Aucun BREAKING.

## [20261031] — v0.1.0 : écarts décidés à la revue LOT-2

- **[CHANGED]** `contracts/models.md` §1 : le dossier du livre est dérivé de `item.Path` (et non de `ContainingFolderPath`).
- **[ADDED]** `contracts/models.md` §7 « Écarts décidés » : année < 1000 ignorée, `dc:date` `creation`/`modification` ignoré,
  OPF sans titre écarté puis repli epub, candidats suivants pour la couverture epub, série sans auteur, index de série borné,
  « pas d'image » = réponse vide (à confirmer en recette emby2), bornes de taille. Aucun BREAKING.

## [20261030] — v0.1.0 : métadonnées et couvertures locales des livres (#6, #7, #8)

- **[ADDED]** `contracts/models.md` — mapping OPF (Calibre/Bindery, OPF 2 et 3) → `Book` Emby 4.10.2 : sources et priorités,
  champs, relecture, couverture (dossier / epub / cbz), configuration, noms stables des providers.
- Aucun endpoint HTTP ni message propre au plugin : la page de configuration utilise l'API standard d'Emby
  (`getPluginConfiguration` / `updatePluginConfiguration`).
- Points **[GATE 2]** (auteur → Tags + tri ; série → Tag + tri) : **validés par l'utilisateur au GATE 2** (2026-10-30) — contrat inchangé, les défauts de `models.md` §2 sont définitifs.
