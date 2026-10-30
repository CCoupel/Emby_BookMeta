# Publish QUALIF — adaptations projet BookMeta

> Compagnon de `publish.qualif.template.md` (mode `promote`) : **prevaut** sur le template. Zero rebuild.

```bash
CANDIDATE="$BUILD_DIR/BookMeta-$VERSION.dll"
test -f "$CANDIDATE" || { echo "Aucun candidat — executer /build d'abord"; exit 1; }
TARGET_DIR="$REPO_ROOT/build/qualif_v$DIR_VERSION"
mkdir -p "$TARGET_DIR"
cp "$CANDIDATE" "$TARGET_DIR/BookMeta-$VERSION.dll"
stat -c %s "$TARGET_DIR/BookMeta-$VERSION.dll"   # taille de reference pour DEPLOY
echo "Publication QUALIF terminee - $VERSION -> $TARGET_DIR/BookMeta-$VERSION.dll"
```

Pas de registre, pas de CI, aucune variable requise. Rollback : `rm -rf "$TARGET_DIR"` puis `/publish qualif`.
