using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// What to scan and which font each language column must be rendered with. Saved as an asset so the setup can be
    /// committed and re-run by the whole team.
    /// </summary>
    [CreateAssetMenu(menuName = "LocaSync/Glyph Scan Profile", fileName = "GlyphScanProfile")]
    public sealed class GlyphScanProfile : ScriptableObject
    {
        public enum SourceKind
        {
            TextAsset,
            FilePath,
            SheetUrl,
        }

        [Tooltip("Where the localization text comes from.")]
        public SourceKind sourceKind = SourceKind.TextAsset;

        [Tooltip("A .csv file imported into the project.")]
        public TextAsset textAsset;

        [Tooltip("Path to a .csv / .tsv file, absolute or relative to the project folder.")]
        public string filePath = string.Empty;

        [Tooltip("A Google Sheets link (shared or published to the web) or any URL that returns CSV/TSV.")]
        public string sheetUrl = string.Empty;

        [Tooltip("Header of the key column. Leave empty to use the first column. Rows with an empty key or a key starting with '#' are skipped.")]
        public string keyColumn = string.Empty;

        [Tooltip("Each language column and the TMP font asset that renders it.")]
        public List<ColumnFontBinding> bindings = new List<ColumnFontBinding>();

        [Tooltip("Ignore TextMeshPro rich text tags such as <b> or <color=#FF0000>.")]
        public bool stripRichTextTags = true;

        [Tooltip("Ignore simple placeholders such as {0}, {name} or {0:N2}.")]
        public bool stripPlaceholders = true;

        [Tooltip("Also search the fallback fonts and default font set in TMP Settings, as TMP does at runtime.")]
        public bool includeTmpSettingsFallbacks = true;

        public ScanOptions ToScanOptions() => new ScanOptions
        {
            StripRichTextTags = stripRichTextTags,
            StripPlaceholders = stripPlaceholders,
        };
    }

    [Serializable]
    public sealed class ColumnFontBinding
    {
        [Tooltip("Column header, e.g. 'ko', 'ja' or 'Japanese(ja)'.")]
        public string column = string.Empty;

        public TMP_FontAsset font;
    }
}
