# Procedure de Test — Smoke QUALIF : plugin BookMeta charge dans emby2

**Version** : 0.1.0.0
**Date** : 2026-10-30
**Testeur** : QA

## Prerequis

- [ ] Environnement : QUALIF (`deployment/emby2`, namespace `media`), `KUBECONFIG=private/kubeconfig.yml` (jamais affiche)
- [ ] Donnees : `BookMeta.dll` candidate deployee dans `/config/plugins/BookMeta.dll`, `rollout restart` effectue
- [ ] Acces : tableau de bord Emby (`emby2`) en administrateur ; la bibliotheque « Livres » n'est pas requise pour ce smoke

## Scenarios

### Scenario 1 — Le plugin est charge sans exception

**Validation** : AUTO — **Issues** : #4

> Joue par `qa` via `kubectl` (commandes + lecture de sortie, sans jugement humain) sur ordre du teamleader.
> Equivalent automatise : le cluster QUALIF n'est pas disponible en CI ; la partie reproductible en CI (type `Plugin`, Id, version, libs SDK)
> est couverte par `Integration/Plugin/Smoke/` et `Integration/Sdk/Version/`.

**Objectif** : Verifier que la DLL est installee, que le pod redemarre sain et que le log ne contient aucune exception BookMeta.

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Comparer la taille de `dist/BookMeta.dll` locale et de `/config/plugins/BookMeta.dll` dans le pod | Tailles identiques | | |
| 2 | `kubectl -n media get pods` apres le restart | Le pod `emby2` est `Running` et `Ready` | | |
| 3 | `grep -i bookmeta /config/logs/embyserver.txt` | Une ligne de chargement du plugin BookMeta ; aucune ligne `Error`/`Exception` mentionnant BookMeta | | |
| 4 | `grep -i -E "exception|error" /config/logs/embyserver.txt` autour du chargement des plugins | Aucune erreur de chargement d'assembly (ex. `FileNotFoundException`, `MissingMethodException`) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 2 — Le plugin est visible dans le tableau de bord

**Validation** : HUMAIN — **Issues** : #4

**Objectif** : Verifier que BookMeta apparait dans la liste des plugins avec le bon nom et la bonne version.

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Ouvrir `Tableau de bord` → `Plugins` | La liste des plugins s'affiche sans erreur | | |
| 2 | Reperer la carte « BookMeta » | Presente, statut actif (non « Erreur » / non desactive) | | |
| 3 | Lire la version affichee | `0.1.0.0` (a ajuster a la version candidate deployee) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 3 — Le plugin survit a un redemarrage

**Validation** : HUMAIN — **Issues** : #4

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | `kubectl -n media rollout restart deployment/emby2` puis attendre `Ready` | Pod `Running` | | |
| 2 | Recharger `Tableau de bord` → `Plugins` | BookMeta toujours liste, meme version | | |
| 3 | Relire `embyserver.txt` depuis le redemarrage | Aucune exception BookMeta | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

## Criteres de Validation

- [ ] Tous les scenarios passent
- [ ] Aucune exception BookMeta dans `embyserver.txt`
- [ ] Aucune regression : les autres plugins d'`emby2` restent charges

## Notes QA

(observations)
