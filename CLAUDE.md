## Project

LocaSync Glyph Scanner: an editor-only Unity package that finds characters in localization tables missing from each language's TextMeshPro font chain. Being built as a free validation MVP before a paid Asset Store product.

- `com.locasync.glyph-scanner/` is the UPM package (the deliverable). `My project/` is a throwaway Unity 6000.6.9f1 sandbox project that references it with `"file:../../com.locasync.glyph-scanner"` and lists it in `testables`. Only the package is shipped. Editor: `C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe`. Batchmode tests need the editor closed, because only one instance can open a project.
- Supports Unity 2022.3 and Unity 6. Write C# 9 at most: block-scoped namespaces, no `record`, no `init`. No async in editor code. Poll with `EditorApplication.update` instead.
- `Editor/Core/` and `Editor/Sources/SheetUrlNormalizer.cs` are pure logic with no TMP or UnityEditor calls. Keep them that way so EditMode tests in `Tests/Editor/` cover them. TMP-specific code lives in `Editor/Tmp/`.
- Namespace `LocaSync.Editor.GlyphScanner`. Inside it the name `Editor` resolves to the namespace, so write `UnityEditor.Editor` in full.
- `versionDefines` set `LOCASYNC_HAS_TMP` when `com.unity.textmeshpro` or `com.unity.ugui` 2.0+ is present. The scanner and test asmdefs require that define. `Editor/MissingTmp/` requires `!LOCASYNC_HAS_TMP` and only shows a "please install TMP" notice. Both cases were verified on 6000.6.9f1 in a fresh project.
- If an open editor says an asmdef "has no scripts associated with it", its asset database is stale. Close the editor and run a batchmode import; don't change the asmdef.
- Fresh-project checks (git URL install, with and without TMP) are faster in a throwaway project created with `Unity.exe -batchmode -createProject`. Batchmode `AssetDatabase.ImportPackage` + `-quit` doesn't finish importing TMP Essential Resources, so unpack that `.unitypackage` (a tar.gz) instead.
- User-facing UI and docs are in English (global Asset Store audience).

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix` (recorded as a `Status:` line in each issue file). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
