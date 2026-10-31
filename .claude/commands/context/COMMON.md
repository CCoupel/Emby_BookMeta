# COMMON — adaptations projet BookMeta

> Compagnon de `COMMON.template.md` : valeurs des placeholders que `/init-project` ne substitue pas.
> Le « serveur » est l'instance Emby de QUALIF (`deployment/emby2`, namespace `media`) ; jamais `emby` (PROD) ici.

| Placeholder du template | Valeur pour BookMeta |
|-------------------------|----------------------|
| `{BUILD_VALIDATE_CMD}` (§2.2) | `test -s dist/BookMeta.dll && stat -c %s dist/BookMeta.dll` |
| `{SERVER_STOP_CMD}` (§3.1) | Sans objet : Emby tourne dans Kubernetes, pas d'arret local — utiliser le redemarrage §3.2 |
| `{SERVER_RESTART_CMD}` (§3.2) | `export KUBECONFIG=private/kubeconfig.yml && kubectl rollout restart deployment/emby2 -n media && kubectl rollout status deployment/emby2 -n media --timeout=120s` |
| `{SERVER_VERIFY_CMD}` (§3.3) | `kubectl -n media exec deploy/emby2 -- sh -c 'ls -l /config/plugins/BookMeta.dll; grep -iE "bookmeta" /config/logs/embyserver.txt \| tail -20'` |
| `{VERSION_READ_CMD}` (§5.6) | `sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/BookMeta/BookMeta.csproj` |
