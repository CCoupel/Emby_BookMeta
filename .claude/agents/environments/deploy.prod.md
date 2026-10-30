# Deploy PROD — adaptations projet BookMeta

> Compagnon de `deploy.prod.template.md` : **remplace** la procedure Helm generique. Meme mecanique que
> `deploy.qualif.md`, mais cible `deployment/emby` (namespace `media`, gere par Helm : ne jamais modifier son
> manifeste) et artefact = asset de la GitHub Release (BORE : jamais la DLL locale de QUALIF).

## GARDE-FOU

Ne s'execute que si l'ordre de l'utilisateur, dans le message courant, demande explicitement le deploiement PROD
**et** la version. Sinon STOP. Jamais enchaine automatiquement apres PUBLISH PROD.

```bash
export KUBECONFIG="${KUBECONFIG:-private/kubeconfig.yml}"
mkdir -p build/prod_v$VERSION
gh release download "v$VERSION" -R CCoupel/Emby_BookMeta -p "BookMeta-$VERSION.dll*" -D "build/prod_v$VERSION" --clobber
(cd build/prod_v$VERSION && sha256sum -c "BookMeta-$VERSION.dll.sha256")
ARTIFACT="build/prod_v$VERSION/BookMeta-$VERSION.dll"
EXPECTED_SIZE=$(stat -c %s "$ARTIFACT")

POD=$(kubectl get pods -n media --no-headers | grep -E '^emby-' | grep -v emby2 | awk '{print $1}')
echo "Pod PROD cible : $POD"   # ne doit pas contenir emby2
MSYS_NO_PATHCONV=1 kubectl exec -n media "$POD" -- cp /config/plugins/BookMeta.dll /config/plugins/BookMeta.dll.prev 2>/dev/null || true
MSYS_NO_PATHCONV=1 kubectl cp "$ARTIFACT" media/$POD:/config/plugins/BookMeta.dll
kubectl rollout restart deployment/emby -n media

kubectl rollout status deployment/emby -n media --timeout=180s
ROLLOUT_STATUS=$?
NEW_POD=$(kubectl get pods -n media --no-headers | grep -E '^emby-' | grep -v emby2 | grep Running | awk '{print $1}')
ACTUAL_SIZE=$(MSYS_NO_PATHCONV=1 kubectl exec -n media "$NEW_POD" -- stat -c %s /config/plugins/BookMeta.dll)
[ "$ACTUAL_SIZE" = "$EXPECTED_SIZE" ] || ROLLOUT_STATUS=1
```

## Echec / Rollback

Restaurer `BookMeta.dll.prev` (ou supprimer `BookMeta.dll` si premier deploiement) puis
`kubectl rollout restart deployment/emby -n media` — commandes de `deploy.qualif.md`, cible `emby`.
