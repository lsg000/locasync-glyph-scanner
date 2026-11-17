# Changelog

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
