# Asset Store listing: LocaSync Glyph Scanner

Copy each section into the matching Publisher Portal field. The text is final copy and ready to paste. Before you submit, fill in the support contact (marked `TODO`).

---

## Publisher
TinyRig

## Package title
LocaSync Glyph Scanner

## Category
Tools / Localization

## Price
Free

## Version
0.1.3 (matches the GitHub release; the changelog is in `Documentation/CHANGELOG.md`)

## Unity version
Upload from Unity 2022.3 (tested on 2022.3.62f3). Also tested on Unity 6000.6.

## Short description / summary
Find the characters in your translations that your TextMeshPro fonts and fallbacks can't render, and copy them straight into the Font Asset Creator. Works with CSV, TSV and Google Sheets.

## Description
Shipping in Japanese, Chinese or Korean? Somewhere in your translations there is a character your static TMP font atlas doesn't have, and players will see it as an empty box: □.

LocaSync Glyph Scanner finds those characters before your players do.

Point it at your localization table: a CSV/TSV file, an export from Unity Localization or I2 Localization, or a Google Sheets link (no API key or OAuth setup). Assign each language column the TextMeshPro font asset that renders it, and click Scan. For each language the scanner walks the same fallback chain TextMeshPro uses at runtime and lists only the characters that none of those fonts contain, de-duplicated and sorted.

One click copies the missing characters, or their hex ranges, ready to paste into the Font Asset Creator. The report also shows which keys use each missing character, so you know exactly what to send back to translation.

**Key features**
- Checks every character against the font asset and its full fallback chain, in TextMeshPro's own search order: the font, its fallbacks depth-first, then the TMP Settings fallbacks and default font.
- Reads CSV and TSV files, text assets, and Google Sheets share or publish links. No API key, no OAuth.
- Ignores TextMeshPro rich text tags (`<b>`, `<color>`, `<sprite>`, `<link>`…) and simple placeholders (`{0}`, `{name}`, `{0:N2}`), so you only see characters that will actually be rendered.
- Handles emoji and other characters outside the Basic Multilingual Plane correctly.
- Copy as characters or as hex ranges for the Font Asset Creator.
- Shows which localization keys use each missing character.
- Reusable scan profiles: commit them and let the whole team re-run the same check.
- Never modifies your font assets, Dynamic fonts included.
- Editor-only. Nothing is added to your builds.

**Works with**
Any localization setup that can export a CSV: Unity Localization, I2 Localization, or your own spreadsheet.

## Technical details
- Requirements: Unity 2022.3 or newer, TextMeshPro (`com.unity.textmeshpro` on 2022.3, `com.unity.ugui` 2.0+ on Unity 6). If TextMeshPro is missing, the tool shows a notice that says what to install. It does not cause compile errors.
- Editor-only. No runtime code and no impact on builds. Works with any render pipeline (Built-in, URP, HDRP), because it doesn't render anything at runtime.
- Menu: Window > LocaSync > Glyph Scanner
- Full C# source included, in its own namespace and assembly definitions.
- Offline documentation: `Documentation/README.md`
- Sample: `Samples/Sample CSV` (English, Korean, Japanese and Simplified Chinese, with rich text, placeholders, a multi-line cell and an emoji)
- Network access: only when you scan a Google Sheets or other URL source. The tool downloads that URL and nothing else. It sends no analytics or telemetry.

## Keywords
localization, textmeshpro, tmp, font, glyph, missing characters, cjk, csv, google sheets, font asset creator

## Support
- Issues: https://github.com/lsg000/locasync-glyph-scanner/issues
- Email: `TODO: publisher support email`

## Release notes (0.1.3)
First Asset Store release. Scans CSV/TSV files and Google Sheets against each language's TextMeshPro font and fallback chain, and copies the missing characters for the Font Asset Creator.

## Notes for the reviewer (Submit dialog)
Editor-only tool, no runtime code. To test:
1. Import the package into a project with TextMeshPro and its Essential Resources.
2. Open Window > LocaSync > Glyph Scanner and click New to create a profile.
3. Set Source to Text Asset and assign `Samples/Sample CSV/sample_localization.csv`.
4. Click Load Columns and add the en, ko, ja and zh-Hans columns with the + buttons. Assign each one `LiberationSans SDF`.
5. Click Scan. LiberationSans has no CJK glyphs, so the ko, ja and zh-Hans columns list their missing characters, and en lists only the emoji.

The tool makes network requests only when the user picks a Sheet URL source.
