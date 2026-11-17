# Changelog

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
