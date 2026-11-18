# Changelog

## [0.1.6] - 2026-11-18

### Fixed
- Case tags are applied. Text inside `<allcaps>`, `<uppercase>` and `<smallcaps>` is checked in upper case, and text inside `<lowercase>` in lower case, as TextMeshPro renders it. Before, a font that had `é` but not `É` passed the scan.
- Text inside `<font="name">` is checked against that font asset and its fallbacks first, and then against the column's font. Before, it was checked against the column's font only and could be reported as missing. The font is looked up where TextMeshPro loads it from (TMP Settings > Default Font Asset path). A name that matches no font asset is reported.
- Characters supplied by sprite assets are no longer reported as missing: the default sprite asset from TMP Settings, and on Unity 6 the emoji fallback assets. Emoji fallbacks are searched first and only for emoji, with TextMeshPro's own rules for the U+FE0F and U+FE0E variation selectors.

### Added
- **Check Uppercase** option: also checks the uppercase form of every letter, for text objects that use the UpperCase or SmallCaps font style (a component setting the scanner cannot see).
- The report has an **Also used** line for fonts from `<font>` tags and emoji fallbacks that supplied characters.
- Scripting API: `FontChainSet` and `GlyphCoverageScanner.Scan(entries, FontChainSet, options)`, `RichTextStripper.Parse`, `TextSanitizer.PrepareRuns`, `CodepointCollector.EnumerateStyled`, `CoverageResult.ExtraSources`, `CoverageResult.Warnings` and `ScanOptions.IncludeUppercaseForms`. The existing `Scan(entries, chain, options)` still works.

### Verified
- Unity 2022.3.62f3 with TextMeshPro 3.0.9 and Unity 6000.6.9f1 with uGUI 2.6.0: all 123 EditMode tests pass. They include scans against TMP's default sprite asset and a `<font>` tag that names a real font asset.

## [0.1.5] - 2026-11-18

### Changed
- Removed a mutable static field (the cached font face) that the Asset Store Validator flags under "Check Static Variables". The cache now belongs to each scan. Behaviour is unchanged; all 89 EditMode tests pass on Unity 2022.3.62f3 and 6000.6.9f1.

## [0.1.4] - 2026-11-18

### Fixed
- Dynamic font assets no longer cause false positives. A Dynamic font adds characters from its source font file at runtime, so the scanner now checks that file too, and characters it can supply are no longer listed as missing. The report shows how many characters each Dynamic font adds at runtime, and **Copy Runtime-Added Characters** copies them in case you prefer to bake them into a static atlas. The font asset itself is not modified.
- A file that is not UTF-8 (for example Excel's plain "CSV" format, which saves as CP949 or Shift-JIS) is now reported with instructions instead of being scanned as garbled text. UTF-16 files with a byte order mark are read.
- A Google Sheets link without a tab id (`gid`) now downloads the first tab. It used to ask for `gid=0`, which fails when that tab has been deleted. HTTP 400 from Google now explains that the tab in the link no longer exists.

### Changed
- Fonts the scanner cannot fully check (Dynamic OS fonts, or a Dynamic font whose source font file is missing) are named in a warning next to the results.
- Scripting API: `IGlyphLookup.IsDynamic` and `CoverageResult.ChainHasDynamicFont` are replaced by `IGlyphLookup.CanAddAtRuntime`, `IGlyphLookup.Caveat`, `CoverageResult.AddedAtRuntime`, `CoverageResult.AddedAtRuntimeCountByChainIndex` and `CoverageResult.Caveats`.

### Verified
- Unity 2022.3.62f3 with TextMeshPro 3.0.9 and Unity 6000.6.9f1 with uGUI 2.6.0: all 89 EditMode tests pass. They include a scan against TMP's default Dynamic fallback font that checks the font asset is left unchanged.

## [0.1.3] - 2026-11-17

### Changed
- Sheet download errors now say what to do. A private, missing or mistyped Google Sheet (HTTP 401/403/404) and a Google sign-in page both explain how to share the sheet. Network failures suggest checking the connection.
- README: recommend CSV over TSV for Google Sheets, because Google's TSV export replaces line breaks inside cells with spaces.

### Verified
- Unity 2022.3.62f3 with TextMeshPro 3.0.9 and Unity 6000.6.9f1 with uGUI 2.6.0. In both, all 77 EditMode tests pass, and without TMP the notice appears with no compile errors.
- Scanning from a Google Sheets share link gives the same results as the local CSV.

## [0.1.2] - 2026-11-17

### Changed
- Each Column → Font binding is now drawn as one row (`column → font field`) instead of a collapsed foldout per binding, so you can assign fonts without expanding every element.

## [0.1.1] - 2026-11-17

### Fixed
- Installing into a project without TextMeshPro no longer causes compile errors. The scanner is left out of the build and a notice (Console warning plus the Window → LocaSync → Glyph Scanner menu item) explains which package to install.
- The "Include TMP Settings Fallbacks" option label no longer gets cut off. It is now called "Global Fallbacks" and has a tooltip.

## [0.1.0] - 2026-11-17

### Added
- Glyph Scanner window (Window → LocaSync → Glyph Scanner) with reusable Glyph Scan Profile assets.
- Sources: CSV TextAsset, CSV/TSV file path, Google Sheets link without an API key (shared or published to the web).
- Per-column coverage check against a TMP font asset and its full fallback chain, including TMP Settings fallbacks.
- Copy missing characters as text or as hex ranges for the TMP Font Asset Creator.
- TMP rich text tag and placeholder filtering; UTF-32 handling for emoji and supplementary-plane characters.
