## Project

LocaSync Glyph Scanner: an editor-only Unity package that finds characters in localization tables missing from each language's TextMeshPro font chain. Being built as a free validation MVP before a paid Asset Store product.

- `com.locasync.glyph-scanner/` is the UPM package (the deliverable). `My project/` is a throwaway Unity 6000.6.9f1 sandbox project that references it with `"file:../../com.locasync.glyph-scanner"` and lists it in `testables`. Only the package is shipped. Editor: `C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe`. Batchmode tests need the editor closed, because only one instance can open a project.
- Supports Unity 2022.3 and Unity 6. Write C# 9 at most: block-scoped namespaces, no `record`, no `init`. No async in editor code. Poll with `EditorApplication.update` instead.
- `Editor/Core/` and `Editor/Sources/SheetUrlNormalizer.cs` are pure logic with no TMP or UnityEditor calls. Keep them that way so EditMode tests in `Tests/Editor/` cover them. TMP-specific code lives in `Editor/Tmp/`.
- Namespace `LocaSync.Editor.GlyphScanner`. Inside it the name `Editor` resolves to the namespace, so write `UnityEditor.Editor` in full.
- The editor asmdef references `Unity.TextMeshPro` directly, with no define constraint. A `versionDefines`-based `LOCASYNC_HAS_TMP` constraint silently excluded the assembly on Unity 6000.6, so don't reintroduce it without testing there.
- User-facing UI and docs are in English (global Asset Store audience).

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix` (recorded as a `Status:` line in each issue file). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
