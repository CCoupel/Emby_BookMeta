#!/usr/bin/env bash
# CI_CMD unique (commands.ci) : appele par ci-branch.yml, release.yml, qa et deploy. Aucune etape propre a la CI.
# En local, dotnet est une install Windows hors PATH (pas de SDK .NET dans WSL) : detecte ou surcharger via DOTNET=.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ -z "${DOTNET:-}" ]; then
  if command -v dotnet >/dev/null 2>&1; then DOTNET=dotnet
  else
    for c in "$HOME/AppData/Local/Microsoft/dotnet/dotnet.exe" /mnt/c/Users/*/AppData/Local/Microsoft/dotnet/dotnet.exe; do
      [ -x "$c" ] && { DOTNET="$c"; break; }
    done
  fi
fi
: "${DOTNET:?dotnet introuvable : definir DOTNET=}"

PROJECT=src/BookMeta/BookMeta.csproj
TESTS=tests/BookMeta.Tests/

echo "== Build Release"
"$DOTNET" build "$PROJECT" --configuration Release --output dist/
test -s dist/BookMeta.dll || { echo "dist/BookMeta.dll absent ou vide"; exit 1; }

echo "== Tests"
"$DOTNET" test "$TESTS" --configuration Release

echo "== Audit des dependances"
AUDIT=$("$DOTNET" list "$PROJECT" package --vulnerable --include-transitive 2>&1)
echo "$AUDIT"
if grep -qiE 'has the following vulnerable packages' <<<"$AUDIT"; then
  echo "Paquets vulnerables detectes"; exit 1
fi

echo "OK : dist/BookMeta.dll ($(wc -c < dist/BookMeta.dll) octets)"
