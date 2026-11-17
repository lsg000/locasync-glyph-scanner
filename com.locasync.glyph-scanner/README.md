# LocaSync Glyph Scanner

Find the characters your translations use that your TextMeshPro fonts can't render, before players see empty boxes.

LocaSync reads your localization table (a CSV/TSV file, or a Google Sheet without an API key). For each language column it checks every character against the TMP font asset you assign to that language and its whole fallback chain. It then lists only the missing characters, de-duplicated and sorted, and copies them for the TMP Font Asset Creator.

- Works with any table you can export as CSV: Unity Localization, I2 Localization, or your own sheet.
- Ignores TMP rich text tags (`<b>`, `<color=#FF0000>`, `<sprite=1>` …) and simple placeholders (`{0}`, `{name}`, `{0:N2}`). Keeps `<noparse>` content.
- Follows the fallback chain the way TMP does at runtime: the font, its fallbacks depth-first, then the TMP Settings fallbacks and default font.
- Handles emoji and other characters outside the Basic Multilingual Plane as single characters.
- Editor-only. Adds nothing to your builds.

## Requirements

- Unity 2022.3 or newer (Unity 6 supported)
- TextMeshPro (`com.unity.textmeshpro`, or `com.unity.ugui` 2.0+ on Unity 6). The tool compiles only when TMP is present.

## Install

Package Manager → **+** → **Add package from git URL…**

```
https://github.com/<you>/<repo>.git?path=/com.locasync.glyph-scanner
```

Or **Add package from disk…** and pick `com.locasync.glyph-scanner/package.json`.

## Use

1. **Window → LocaSync → Glyph Scanner**, then click **New** to create a profile.
2. Choose a source:
   - **Text Asset**: a `.csv` file inside your project.
   - **File Path**: any `.csv` / `.tsv` file, for example an export from Unity Localization or I2.
   - **Sheet URL**: a Google Sheets link. Either use *File → Share → Publish to web*, or set *General access* to *Anyone with the link*. No OAuth or API key is needed.
3. Click **Load Columns**, then use the **+ column** buttons to bind each language column to its TMP font asset.
4. Click **Scan**. For each column you get the font chain, how many characters each font covers, and the missing characters.
5. Click **Copy Characters**. In the Font Asset Creator, set *Character Set* to *Custom Characters*, paste, and regenerate the atlas. You can also click **Copy Hex Ranges** and paste into *Unicode Range (Hex)*.

The first row must be a header row. Rows with an empty key, or a key starting with `#`, are skipped. Columns are matched by header name, ignoring case.

## Notes

- **Dynamic fonts**: a Dynamic font asset adds glyphs from its source font file at runtime. A character reported missing from a chain that contains a Dynamic font may still render, but only if the source font file has it. The scanner tells you when this applies.
- **Plural / select forms** such as `{0:plural:item|items}` are scanned as written, because their words are rendered.
- The scanner reads the font asset's character table directly. It does not call `HasCharacters` with `tryAddCharacter`, so scanning never changes your font assets.
