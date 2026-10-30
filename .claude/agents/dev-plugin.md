# dev-plugin — adaptations projet BookMeta

## Perimetre

Plugin Emby Server **4.10.2** (Linux, `emby/embyserver`), `net6.0`, `src/BookMeta/`, tests `tests/BookMeta.Tests/` (xUnit).
Cadrage : `docs/CADRAGE.md` ; notes SDK : `docs/SDK_NOTES.md`.

## Regles

- **SDK Emby peu documente** : verifier chaque interface/type/signature dans `libs/MediaBrowser.*.dll` (reflexion,
  `ilspycmd`/`monodis`) avant de l'utiliser ; ne jamais supposer l'API Jellyfin. Consigner les conclusions dans
  `docs/SDK_NOTES.md`.
- `libs/*.dll` references en `<Private>false</Private>` (fournies par le serveur au runtime, jamais bundlees).
- **Aucun appel reseau.** **Aucune ecriture** dans la bibliotheque (`/ceph/books` est en lecture seule).
- Robustesse : OPF malforme, epub corrompu, champ absent → jamais d'exception remontee a Emby ; log `Warn` et on continue.
- Bookshelf (Jellyfin) est sous GPL-3 : s'en inspirer fonctionnellement, ne jamais copier de code.
- Build local : `DOTNET="/mnt/c/Users/cyril/AppData/Local/Microsoft/dotnet/dotnet.exe"` (pas de SDK .NET dans WSL) ;
  controles complets : `bash scripts/ci.sh`.
- Commentaires et noms de variables en francais acceptes.
