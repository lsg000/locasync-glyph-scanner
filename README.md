# LocaSync Glyph Scanner

Find the characters your translations use that your TextMeshPro fonts can't render, before players see empty boxes.

LocaSync reads your localization table (a CSV/TSV file, or a Google Sheet without an API key). For each language column it checks every character against the TMP font asset you assign to that language and its whole fallback chain. It then lists only the missing characters, de-duplicated and sorted, ready to paste into the TMP Font Asset Creator.

- Works with any table you can export as CSV: Unity Localization, I2 Localization, or your own sheet.
- Ignores TMP rich text tags and simple placeholders (`{0}`, `{name}`).
- Follows the fallback chain the way TMP does at runtime, including the TMP Settings fallbacks.
- Handles emoji and other characters outside the Basic Multilingual Plane.
- Editor-only. Adds nothing to your builds.

## Install

Unity 2022.3+ or Unity 6, with TextMeshPro. Package Manager → **+** → **Add package from git URL…**

```
https://github.com/lsg000/locasync-glyph-scanner.git?path=/com.locasync.glyph-scanner#v0.1.1
```

Then open **Window → LocaSync → Glyph Scanner**. See the [package README](com.locasync.glyph-scanner/README.md) for the full walkthrough.

## Repository layout

- `com.locasync.glyph-scanner/`: the package (code, EditMode tests, sample CSV).
- `My project/`: the Unity sandbox used for development. Only its settings and package manifest are tracked.

## Feedback

This is an early release. Issues and feature requests are welcome: what localization setup you use, which languages, and what the scanner missed.

## License

[MIT](LICENSE)
