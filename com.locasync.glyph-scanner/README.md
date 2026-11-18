# LocaSync Glyph Scanner

Find the characters your translations use that your TextMeshPro fonts can't render, before players see empty boxes.

LocaSync reads your localization table (a CSV/TSV file, or a Google Sheet without an API key). For each language column it checks every character against the TMP font asset you assign to that language and its whole fallback chain. It then lists only the missing characters, de-duplicated and sorted, and copies them for the TMP Font Asset Creator.

- Works with any table you can export as CSV: Unity Localization, I2 Localization, or your own sheet.
- Ignores TMP rich text tags (`<b>`, `<color=#FF0000>`, `<sprite=1>` …) and simple placeholders (`{0}`, `{name}`, `{0:N2}`). Keeps `<noparse>` content.
- Follows the fallback chain the way TMP does at runtime: the font, its fallbacks depth-first, then the TMP Settings fallbacks and default font.
- Understands Dynamic font assets: characters a Dynamic font adds from its source font file at runtime are not reported as missing.
- Handles emoji and other characters outside the Basic Multilingual Plane as single characters.
- Editor-only. Adds nothing to your builds.

## Requirements

- Unity 2022.3 or newer (Unity 6 supported)
- TextMeshPro (`com.unity.textmeshpro`, or `com.unity.ugui` 2.0+ on Unity 6). Most templates include it. If it is missing, the package shows a notice that tells you what to install, instead of compile errors.

## Install

- **Asset Store**: import the package. Everything goes into `Assets/LocaSync Glyph Scanner/`.
- **Git (UPM)**: Package Manager → **+** → **Add package from git URL…**, then enter `https://github.com/lsg000/locasync-glyph-scanner.git?path=/com.locasync.glyph-scanner`

Install it one way only. Both copies use the same assembly names and GUIDs, so installing both causes conflicts.

To try it without your own data, use the sample CSV: Package Manager → *Samples* (git install), or `Samples/` in the Asset Store folder.

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

- **Dynamic fonts**: a Dynamic font asset adds glyphs from its source font file at runtime, so the scanner checks that file too. Characters it can supply are not reported as missing, and the report shows how many each Dynamic font adds at runtime. A Dynamic font with a single atlas texture stops adding characters once the texture is full, so turn on *Multi Atlas Textures* if you rely on it for many characters.
- **Dynamic OS fonts** take glyphs from fonts installed on the player's device. That cannot be checked in the editor, so only the characters already in the atlas are counted, and the scanner warns you when this applies.
- **Encoding**: files must be UTF-8, or UTF-16 with a byte order mark. In Excel, save as *CSV UTF-8*. A file in a legacy encoding (CP949, Shift-JIS …) is reported instead of being scanned as garbled text.
- **Plural / select forms** such as `{0:plural:item|items}` are scanned as written, because their words are rendered.
- **Google Sheets: prefer CSV.** Share links are fetched as CSV automatically. If you publish to the web, choose CSV rather than TSV: Google's TSV export turns line breaks inside a cell into spaces, while CSV keeps them.
- The scanner reads the font asset's character table and, for Dynamic fonts, the source font file. It does not call `HasCharacters` with `tryAddCharacter`, so scanning never changes your font assets.
