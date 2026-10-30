# Deploy — adaptations projet BookMeta

> Compagnon de `deploy.template.md`. Les regles ci-dessous **prevalent** sur le template.
> Repris d'Emby_Badges (meme serveur, meme mecanique). Procedures par environnement : `environments/{publish,deploy}.<env>.md`.

## Contexte projet

- Artefact unique : `BookMeta.dll` (plugin Emby, `net6.0`). Pas de Docker, pas d'image, pas de Helm.
- Outillage (hors PATH, shell de reference WSL — pas de SDK .NET dans WSL) :
  `DOTNET="/mnt/c/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe"` (SDK 6.0.428).
- Tests : projet xUnit `tests/BookMeta.Tests/` (`commands.test`). La recette fonctionnelle se fait en QUALIF (`emby2`).
- Version : `<Version>` de `src/BookMeta/BookMeta.csproj` (`version_file`). Lue comme `X.Y.Z.0` si 3 champs, ecrite
  au format `X.Y.Z.a`. Extraction : `sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/BookMeta/BookMeta.csproj`.
  Ecriture : remplacer uniquement le contenu de la balise `<Version>`.

## Tache BUILD (remplace l'etape 3 du template)

Etapes 1-2 du template inchangees (sans `npm test`). Etape 3 :

```bash
DOTNET="/mnt/c/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe"
REPO_ROOT=$(git rev-parse --show-toplevel)
BUILD_DIR="$REPO_ROOT/build/candidate_v$DIR_VERSION"
mkdir -p "$BUILD_DIR/tmp"
# dotnet.exe est un binaire Windows : --output doit recevoir un chemin Windows
"$DOTNET" build src/BookMeta/BookMeta.csproj --configuration Release \
  -p:Version=$VERSION --output "$(wslpath -w "$BUILD_DIR/tmp")"
test -s "$BUILD_DIR/tmp/BookMeta.dll" || { echo "BUILD FAILED : BookMeta.dll absent"; exit 1; }
cp "$BUILD_DIR/tmp/BookMeta.dll" "$BUILD_DIR/BookMeta-$VERSION.dll"
rm -rf "$BUILD_DIR/tmp"
```

Notification : `Candidat : build/candidate_v$DIR_VERSION/BookMeta-$VERSION.dll` + taille (`stat -c %s`).

## Variables d'environnement

`KUBECONFIG` = `private/kubeconfig.yml` par defaut (gitignore, copie de `INFRA/Infra-K8S/.config/kubeconfig`).
Ne jamais afficher ce fichier. Cle API Emby (verification par API `https://coupel.net/emby`) : fournie par
l'utilisateur, jamais ecrite dans le depot.

## Regles absolues

- **Jamais de deploiement sur `emby` (production) sans ordre explicite de l'utilisateur** dans le message courant.
- **Aucune ecriture dans `/ceph/books`** (monte en lecture seule ; le plugin ne doit jamais y ecrire).
- Ne pas toucher aux DLL des autres plugins (`EmbyBadges.dll`, `VirtualLib.dll`...).
