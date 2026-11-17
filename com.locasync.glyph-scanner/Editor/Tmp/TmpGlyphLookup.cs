using System.Collections.Generic;
using TMPro;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Looks characters up in a TMP_FontAsset's character table.
    /// The table is read directly instead of calling TMP_FontAsset.HasCharacters because
    /// (1) some TMP versions' HasCharacter(s) overloads work per UTF-16 char and miss supplementary-plane characters
    /// such as emoji, and (2) HasCharacters with tryAddCharacter would modify Dynamic font assets during a scan.
    /// </summary>
    internal sealed class TmpGlyphLookup : IGlyphLookup
    {
        readonly TMP_FontAsset _font;

        public TmpGlyphLookup(TMP_FontAsset font)
        {
            _font = font;
        }

        public string Name => _font != null ? _font.name : "<missing font>";

        public bool IsDynamic => _font != null && _font.atlasPopulationMode != AtlasPopulationMode.Static;

        public bool Contains(uint codepoint)
        {
            if (_font == null) return false;
            Dictionary<uint, TMP_Character> table = _font.characterLookupTable;
            return table != null && table.ContainsKey(codepoint);
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
            var lookups = new List<IGlyphLookup>();
            foreach (TMP_FontAsset font in fonts) lookups.Add(new TmpGlyphLookup(font));
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
