# Memoire projet — BookMeta

> Source de verite unique au demarrage d'une session (/start-session). Mise a jour par /end-session.

## Etat courant

- **Version** : 1.0.0 (PROD `deployment/emby`, `.prev` = 0.1.0 ; QUALIF emby2 = 1.0.0.1)
- **Releases** : v0.1.0 et v1.0.0 publiées sur GitHub
- **Branche** : `main` (tag v1.0.0 sur 72a636a ; commits mémoire/template après) ; `gh-pages` = site marketing https://CCoupel.github.io/Emby_BookMeta/ (worktree `MARKETING/`)
- **Travail en cours** : aucun. Milestones v0.1.0 et v1.0.0 fermés ; aucune issue ouverte.
- **Cadrage produit** : `docs/CADRAGE.md`
- **Template Claude** : v3.29.2 (sync 2026-10-31, `a3770a4`) — `/end-session` étape 4bis et `/start-session` 5bis classent les branches résiduelles

## Regles critiques

- QUALIF = `deployment/emby2` ; PROD = `deployment/emby` uniquement sur ordre explicite (namespace `media`).
- Recette UI : QUALIF emby2 via http://media.coupel.net/emby (session Chrome) ; jamais coupel.net/emby (PROD) pour la recette.
- Log Emby : `/config/logs/embyserver*.txt`.
- Aucun appel réseau, aucune écriture dans la bibliothèque ; SDK Emby vérifié dans `libs/` (jamais supposé depuis Jellyfin).
- Pas de `git commit --amend` sur branche partagée.
- Release via `release.yml` sur tag ; compatibilité testée Emby 4.10.2 uniquement.

## Decisions

- `metadata.opf` source de vérité ; aucun export `.nfo` (ni plugin, ni saver NFO d'Emby) — hors périmètre.
- Auteur → Tag + tri « Nom, Prénom - titre » ; série → Tag « Série : X » + tri « Auteur - Série NN - Titre ».
- Tri verrouillé (`BookSortNameProvider`, `IForcedProvider`) : réécrit à chaque actualisation ; le verrou reste après désinstallation (procédure de retour dans le README).
- Genres : champ Genres uniquement (pas de tags « Genre : X »).
- Emby n'offre pas d'onglets Auteurs / Genres pour les livres (Livres / Mot-clé / Dossiers seulement).
- Couvertures : « BookMeta Embedded Cover » activé automatiquement une fois par bibliothèque de livres (`AutoEnabledLibraryIds`) ; PDF sans couverture.
- Dépôt public, aucune licence (tous droits réservés).

## Corrections de comportement

- Vérifier soi-même (Chrome / API) avant de demander une validation ; ne demander à l'utilisateur que ce qui est invérifiable.
