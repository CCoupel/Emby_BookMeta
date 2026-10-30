# Changelog des contrats

> Changements de contrats (API, schemas) : `BREAKING` | `CHANGED` | `ADDED`. Lu par le CDP au GATE 2.

## [20261030] — v0.1.0 : métadonnées et couvertures locales des livres (#6, #7, #8)

- **[ADDED]** `contracts/models.md` — mapping OPF (Calibre/Bindery, OPF 2 et 3) → `Book` Emby 4.10.2 : sources et priorités,
  champs, relecture, couverture (dossier / epub / cbz), configuration, noms stables des providers.
- Aucun endpoint HTTP ni message propre au plugin : la page de configuration utilise l'API standard d'Emby
  (`getPluginConfiguration` / `updatePluginConfiguration`).
- Points **[GATE 2]** (auteur → Tags + tri ; série → Tag + tri) : **validés par l'utilisateur au GATE 2** (2026-10-30) — contrat inchangé, les défauts de `models.md` §2 sont définitifs.
