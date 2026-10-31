# Procedure de Test — Page de configuration BookMeta (LOT-3, #8)

**Version** : version deployee en QUALIF (`X.Y.Z.a`)
**Date** : [date de la recette]
**Testeur** : QA (orchestrateur : Chrome + kubectl)

## Prerequis

- [ ] Environnement : QUALIF (`deployment/emby2`, namespace `media`), plugin BookMeta de la version LOT-3 deploye (`/deploy qualif`)
- [ ] Session administrateur ouverte dans Chrome sur `emby2` ; `KUBECONFIG=private/kubeconfig.yml` (jamais affiche) ; cle API fournie par l'utilisateur (jamais ecrite)
- [ ] Bibliotheque « Livres » presente, recette LOT-2 validee (recuperateur `BookMeta Embedded Cover` coche)
- [ ] **Etat initial releve** (pour la remise en etat en fin de procedure) : contenu de `/config/plugins/configurations/BookMeta.xml`
      (`kubectl exec ... cat`), en particulier la valeur de `EnableEpubCoverExtraction` et la liste `AutoEnabledLibraryIds`
- Selecteurs / ids HTML de la page : voir le handoff dev-plugin du LOT-3 (`_work/handoff/dev-plugin-*.md`).
  Maquette de reference : `_work/mockup/0.1.0/ui/config-page__epub-cover-toggle.html`.

## Scenarios

### Scenario 1 — La page BookMeta est accessible depuis le menu

**Validation** : AUTO — **Issues** : #8

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Chrome : ouvrir le menu du tableau de bord (section serveur) | L'entree « BookMeta » est presente dans le menu | | |
| 2 | Cliquer sur « BookMeta » | La page s'affiche sans erreur ; aucune erreur dans la console du navigateur (`read_console_messages`) | | |
| 3 | Lire le contenu de la page | Titre « BookMeta » ; une seule case « Extraire la couverture depuis le fichier epub » ; un bouton d'enregistrement ; texte d'aide court | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 2 — Etat par defaut : case cochee

**Validation** : AUTO — **Issues** : #8

**Objectif** : Avec un `BookMeta.xml` neuf (ou `EnableEpubCoverExtraction=true`), la case est affichee cochee.

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Si l'etat initial a l'option desactivee, noter la valeur et la remettre a `true` par la page (scenario 3) avant ce test | Option `true` | | |
| 2 | Ouvrir la page, lire l'etat de la case (`checked`) | Cochee | | |
| 3 | `ApiClient.getPluginConfiguration('097ed1ba-f0ae-496f-8f56-e54599fe54b1')` (console de la page) | `EnableEpubCoverExtraction: true` | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 3 — Decocher, enregistrer, persistance apres redemarrage

**Validation** : AUTO — **Issues** : #8

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Decocher la case puis cliquer sur Enregistrer | Message de confirmation standard d'Emby ; aucune erreur console | | |
| 2 | `kubectl exec ... cat /config/plugins/configurations/BookMeta.xml` | `<EnableEpubCoverExtraction>false</EnableEpubCoverExtraction>` | | |
| 3 | Dans ce meme fichier, comparer `AutoEnabledLibraryIds` a l'etat initial releve | **Identique** (aucun identifiant perdu ni ajoute par l'enregistrement) | | |
| 4 | `kubectl -n media rollout restart deployment/emby2`, attendre `Ready` | Pod `Running` | | |
| 5 | Recharger la page BookMeta | Case **decochee** (valeur persistee) | | |
| 6 | `grep -i bookmeta` du log Emby le plus recent | Aucune exception BookMeta | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 4 — Option desactivee : pas de couverture epub apres refresh

**Validation** : AUTO — **Issues** : #8

**Objectif** : Option decochee (etat du scenario 3). Les epub sans `cover.*` / `folder.*` perdent leur couverture embarquee ; le `.cbz` la garde.

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Lancer `POST Items/{libId}/Refresh?Recursive=true&MetadataRefreshMode=FullRefresh&ImageRefreshMode=FullRefresh&ReplaceAllMetadata=true&ReplaceAllImages=true` ; attendre la fin du scan | Scan termine | | |
| 2 | Pour les epub de reference (Tentation, Fascination…), `GET /Items/{id}` puis `ImageTags` | Pas de tag d'image `Primary` (aucune couverture) | | |
| 3 | Item `.cbz` de Frankenwood | Tag `Primary` present, `GET /Items/{id}/Images/Primary` = HTTP 200 | | |
| 4 | Metadonnees des epub (Name, SortName, Tags, Overview) | Inchangees par rapport a la recette LOT-2 (l'option ne concerne que la couverture) | | |
| 5 | Log Emby | Aucune exception BookMeta | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 5 — Reactivation et remise a l'etat initial

**Validation** : AUTO — **Issues** : #8

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Rouvrir la page, recocher la case, Enregistrer | Confirmation standard | | |
| 2 | `cat BookMeta.xml` | `EnableEpubCoverExtraction=true` ; `AutoEnabledLibraryIds` toujours identique a l'etat initial | | |
| 3 | Relancer un refresh complet (comme scenario 4, etape 1) | Scan termine | | |
| 4 | `ImageTags` des epub de reference | Tag `Primary` de nouveau present, image non vide | | |
| 5 | Comparer `BookMeta.xml` a l'etat initial releve en prerequis | Memes valeurs que l'etat initial (si l'etat initial avait l'option a `false`, la remettre a `false`) | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

### Scenario 6 — Conformite visuelle a la maquette

**Validation** : HUMAIN — **Issues** : #8

**Objectif** : Jugement visuel que Chrome ne peut pas porter : la page est-elle coherente avec la maquette et l'interface Emby ?

| Etape | Action | Resultat Attendu | Resultat Obtenu | OK ? |
|-------|--------|-----------------|----------------|------|
| 1 | Comparer la page rendue a `_work/mockup/0.1.0/ui/config-page__epub-cover-toggle.html` | Meme structure (titre, case, texte d'aide, bouton) | | |
| 2 | Juger la lisibilite du libelle et du texte d'aide | Comprehensibles sans documentation | | |
| 3 | Juger l'integration dans le theme Emby (clair/sombre) | Pas de rupture visuelle manifeste | | |

**Verdict** : [ ] PASS  [ ] FAIL

---

## Criteres de Validation

- [ ] Scenarios 1 a 5 passent
- [ ] `AutoEnabledLibraryIds` identique avant/apres toute operation de la page
- [ ] Aucune exception BookMeta dans le log Emby
- [ ] `BookMeta.xml` remis a l'etat initial en fin de procedure
- [ ] Scenario 6 valide par l'utilisateur au GATE QUALIF

## Notes QA

(observations)
