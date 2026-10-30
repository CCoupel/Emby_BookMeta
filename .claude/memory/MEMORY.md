# Memoire projet — BookMeta

> Source de verite unique au demarrage d'une session (/start-session). Mise a jour par /end-session.

## Etat courant

- **Version** : 0.1.0
- **Branche** : main
- **Travail en cours** : aucun — projet initialise par /init-project
- **Issues actives** : aucune
- **Cadrage produit** : docs/CADRAGE.md (si present — workshop ou brief)

## Regles critiques

- QUALIF = `deployment/emby2` ; PROD = `deployment/emby` uniquement sur ordre explicite (namespace `media`).
- `/ceph/books` monte en lecture seule dans `emby2` le 2026-10-30 par /init-project (patch kubectl, emby2 non gere par Helm ;
  manifeste precedent : `private/emby2-before-books-mount.yaml`). Bibliotheque « Livres » a creer dans l'UI d'emby2 avant la 1re recette.
- Aucun appel reseau, aucune ecriture dans la bibliotheque ; SDK Emby verifie dans `libs/` (jamais suppose depuis Jellyfin).
- Okuzukey (plugin Emby communautaire, beta, Windows, source fermee) couvre deja une partie du besoin : voir `docs/CADRAGE.md`.

## Corrections de comportement

(aucune pour le moment)
