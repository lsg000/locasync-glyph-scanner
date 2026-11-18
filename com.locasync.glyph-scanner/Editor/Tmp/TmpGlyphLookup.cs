using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Answers "can this TMP font asset render the character?" without changing the font asset.
    ///
    /// The character table is read directly instead of calling TMP_FontAsset.HasCharacters because
    /// (1) some TMP versions' HasCharacter(s) overloads work per UTF-16 char and miss supplementary-plane characters
    /// such as emoji, and (2) HasCharacters with tryAddCharacter would add glyphs to Dynamic font assets during a scan.
    ///
    /// A Dynamic font asset also renders characters that are not in its table yet: at runtime TMP adds them from the
    /// source font file. For those the source font file is asked directly (FontEngine), which reads the file and
    /// leaves the font asset untouched.
    /// </summary>
    internal sealed class TmpGlyphLookup : IGlyphLookup
    {
        // TMP gives these characters a stand-in glyph when the source font file has none of its own.
        const uint NoBreakSpace = 0xA0, Space = 0x20, SoftHyphen = 0xAD, NonBreakingHyphen = 0x2011, HyphenMinus = 0x2D;

        readonly TMP_FontAsset _font;
        readonly FontFaceLoader _faces;
        readonly Font _sourceFont;

        /// <param name="faces">Shared by the lookups of one chain, so they agree on which font face is loaded.</param>
        public TmpGlyphLookup(TMP_FontAsset font, FontFaceLoader faces = null)
        {
            _font = font;
            _faces = faces ?? new FontFaceLoader();
            if (font == null) return;

            switch (font.atlasPopulationMode)
            {
                case AtlasPopulationMode.Static:
                    break;

                case AtlasPopulationMode.Dynamic:
                    _sourceFont = FindSourceFont(font);
                    if (_sourceFont == null || !_faces.Load(_sourceFont))
                    {
                        _sourceFont = null;
                        Caveat = "'" + font.name + "' is a Dynamic font asset, but its source font file could not be read, " +
                                 "so only the characters already in its atlas were checked. It may render more at runtime.";
                    }
                    break;

                default: // Dynamic OS (newer TMP versions): glyphs come from fonts installed on the player's device.
                    Caveat = "'" + font.name + "' uses Dynamic OS mode: at runtime it takes glyphs from fonts installed " +
                             "on the player's device, which cannot be checked in the editor. Only the characters " +
                             "already in its atlas were checked.";
                    break;
            }
        }

        public string Name => _font != null ? _font.name : "<missing font>";

        public string Caveat { get; }

        public bool Contains(uint codepoint)
        {
            if (_font == null) return false;
            Dictionary<uint, TMP_Character> table = _font.characterLookupTable;
            return table != null && table.ContainsKey(codepoint);
        }

        public bool CanAddAtRuntime(uint codepoint)
        {
            if (_sourceFont == null || !_faces.Load(_sourceFont)) return false;
            if (HasGlyph(codepoint)) return true;

            switch (codepoint)
            {
                case NoBreakSpace:
                    return HasGlyph(Space);
                case SoftHyphen:
                case NonBreakingHyphen:
                    return HasGlyph(HyphenMinus);
                default:
                    return false;
            }
        }

        static bool HasGlyph(uint codepoint) => FontEngine.TryGetGlyphIndex(codepoint, out uint glyphIndex) && glyphIndex != 0;

        // A Dynamic font asset normally references its source font directly. Some assets only keep the GUID of the
        // source file (TMP restores the reference from it in the editor), so fall back to that.
        static Font FindSourceFont(TMP_FontAsset font)
        {
            if (font.sourceFontFile != null) return font.sourceFontFile;

            SerializedProperty guid = new SerializedObject(font).FindProperty("m_SourceFontFileGUID");
            if (guid == null || string.IsNullOrEmpty(guid.stringValue)) return null;

            string path = AssetDatabase.GUIDToAssetPath(guid.stringValue);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Font>(path);
        }
    }

    /// <summary>
    /// FontEngine works on one "current" font face. This remembers which face was loaded last, so a scan does not
    /// reload the face for every character. Create one per scan and use its lookups right away: TextMeshPro loads
    /// its own face whenever it needs one (which also makes changing the current face here safe), so an old loader
    /// no longer knows what is current.
    /// </summary>
    internal sealed class FontFaceLoader
    {
        Font _loaded;

        public bool Load(Font font)
        {
            if (ReferenceEquals(_loaded, font)) return true;
            if (FontEngine.LoadFontFace(font) != FontEngineError.Success)
            {
                _loaded = null;
                return false;
            }
            _loaded = font;
            return true;
        }
    }

    /// <summary>Builds the fallback chain TMP searches for a font asset.</summary>
    internal static class TmpFontChain
    {
        public static List<TMP_FontAsset> Resolve(TMP_FontAsset primary, bool includeGlobalFallbacks)
        {
            if (primary == null) return new List<TMP_FontAsset>();
            return FontChainResolver.Resolve(primary, GetFallbacks, includeGlobalFallbacks ? GetGlobalFallbacks() : null);
        }

        public static List<IGlyphLookup> ToLookups(IEnumerable<TMP_FontAsset> fonts)
        {
            var faces = new FontFaceLoader();
            var lookups = new List<IGlyphLookup>();
            foreach (TMP_FontAsset font in fonts) lookups.Add(new TmpGlyphLookup(font, faces));
            return lookups;
        }

        // Filter with UnityEngine.Object's == so destroyed / missing references are skipped.
        static IEnumerable<TMP_FontAsset> GetFallbacks(TMP_FontAsset font)
        {
            List<TMP_FontAsset> table = font.fallbackFontAssetTable;
            if (table == null) yield break;
            foreach (TMP_FontAsset fallback in table)
            {
                if (fallback != null) yield return fallback;
            }
        }

        // TMP searches the project-wide fallbacks from TMP Settings, then the default font asset, after a font's own chain.
        static IEnumerable<TMP_FontAsset> GetGlobalFallbacks()
        {
            if (TMP_Settings.instance == null) yield break;

            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks != null)
            {
                foreach (TMP_FontAsset fallback in fallbacks)
                {
                    if (fallback != null) yield return fallback;
                }
            }

            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) yield return defaultFont;
        }
    }
}
