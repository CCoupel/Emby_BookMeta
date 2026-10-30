# Deploy QUALIF — adaptations projet BookMeta

> Compagnon de `deploy.qualif.template.md` : **remplace integralement** la procedure Helm generique
> (le plugin est une DLL copiee dans le volume `/config/plugins` du pod). Cible : `deployment/emby2`,
> namespace `media`. **Jamais `emby`** (production).
> `/ceph/books` est monte en lecture seule dans `emby2` depuis le 2026-10-30 (subPath `books` de `media-media-claim`,
> comme `emby`) ; manifeste precedent : `private/emby2-before-books-mount.yaml`.

## Regles critiques

1. `export KUBECONFIG=private/kubeconfig.yml` en premier.
2. Destination : `/config/plugins/BookMeta.dll` a la racine du dossier plugins (sans version dans le nom).
3. `MSYS_NO_PATHCONV=1` pour tout argument kubectl contenant un chemin Linux absolu (si Git Bash).
4. Source = chemin relatif a la racine du repo (`build/qualif_v.../...`), jamais un chemin Windows.
5. Copier AVANT le restart ; verifier la taille dans le NOUVEAU pod.

```bash
export KUBECONFIG="${KUBECONFIG:-private/kubeconfig.yml}"
ARTIFACT="build/qualif_v$DIR_VERSION/BookMeta-$VERSION.dll"
test -f "$ARTIFACT" || { echo "Publication QUALIF absente pour $VERSION — /publish qualif d'abord"; exit 1; }
EXPECTED_SIZE=$(stat -c %s "$ARTIFACT")

POD=$(kubectl get pods -n media --no-headers | grep '^emby2-' | awk '{print $1}')
MSYS_NO_PATHCONV=1 kubectl exec -n media "$POD" -- cp /config/plugins/BookMeta.dll /config/plugins/BookMeta.dll.prev 2>/dev/null || true
MSYS_NO_PATHCONV=1 kubectl cp "$ARTIFACT" media/$POD:/config/plugins/BookMeta.dll
kubectl rollout restart deployment/emby2 -n media

kubectl rollout status deployment/emby2 -n media --timeout=120s
ROLLOUT_STATUS=$?
NEW_POD=$(kubectl get pods -n media --no-headers | grep '^emby2-' | grep Running | awk '{print $1}')
ACTUAL_SIZE=$(MSYS_NO_PATHCONV=1 kubectl exec -n media "$NEW_POD" -- stat -c %s /config/plugins/BookMeta.dll)
[ "$ACTUAL_SIZE" = "$EXPECTED_SIZE" ] || ROLLOUT_STATUS=1
```

**Smoke test** : taille identique, pod `Running`, et dans `/config/logs/embyserver.txt` le chargement du plugin
(`grep -i bookmeta`) sans exception. La recette (titre, auteur, resume, annee, couverture des livres de reference
apres « Actualiser les metadonnees » de la bibliotheque Livres) est faite au GATE QUALIF.
Prerequis unique : bibliotheque « Livres » (`/ceph/books`) creee dans l'UI d'`emby2`.

## Echec / Rollback

`kubectl rollout undo` est inoperant (DLL sur volume). Restaurer `.prev` (ou supprimer la DLL si premier deploiement) :

```bash
POD=$(kubectl get pods -n media --no-headers | grep '^emby2-' | awk '{print $1}')
MSYS_NO_PATHCONV=1 kubectl exec -n media "$POD" -- sh -c 'if [ -f /config/plugins/BookMeta.dll.prev ]; then cp /config/plugins/BookMeta.dll.prev /config/plugins/BookMeta.dll; else rm -f /config/plugins/BookMeta.dll; fi'
kubectl rollout restart deployment/emby2 -n media
```
