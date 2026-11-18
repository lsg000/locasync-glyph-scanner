using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

        /// <param name="faces">Shared by the lookups of one scan, so they agree on which font face is loaded.</param>
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

        public TMP_FontAsset Font => _font;

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
    /// A TMP sprite asset. TextMeshPro falls back to sprites that have a Unicode value assigned, which is how the
    /// default sprite asset and emoji sprite assets supply emoji.
    /// </summary>
    internal sealed class TmpSpriteLookup : IGlyphLookup
    {
        readonly TMP_SpriteAsset _asset;

        public TmpSpriteLookup(TMP_SpriteAsset asset)
        {
            _asset = asset;
        }

        public string Name => _asset.name + " (sprites)";

        public string Caveat => null;

        public bool Contains(uint codepoint)
        {
            Dictionary<uint, TMP_SpriteCharacter> table = _asset.spriteCharacterLookupTable;
            return table != null && table.ContainsKey(codepoint);
        }

        public bool CanAddAtRuntime(uint codepoint) => false;
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

    /// <summary>
    /// Emoji fallbacks exist only in newer TextMeshPro versions (com.unity.ugui 2.x), and the tests TMP uses to decide
    /// what counts as an emoji are internal. Both are reached by reflection so the same code compiles everywhere.
    /// </summary>
    internal static class TmpEmojiSupport
    {
        const uint FirstPictograph = 0x1F000, LastPictograph = 0x1FAFF;

        static readonly PropertyInfo FallbackAssetsProperty =
            typeof(TMP_Settings).GetProperty("emojiFallbackTextAssets", BindingFlags.Public | BindingFlags.Static);

        static readonly Func<uint, bool> PresentationTest = Bind("IsEmojiPresentationForm");
        static readonly Func<uint, bool> EmojiTest = Bind("IsEmoji");

        /// <summary>The emoji fallback assets from TMP Settings, or an empty list on TMP versions without them.</summary>
        public static List<TMP_Asset> FallbackAssets()
        {
            var assets = new List<TMP_Asset>();
            if (FallbackAssetsProperty == null) return assets;

            if (FallbackAssetsProperty.GetValue(null) is IEnumerable values)
            {
                foreach (object value in values)
                {
                    // UnityEngine.Object's == so destroyed / missing references are skipped.
                    if (value is TMP_Asset asset && asset != null) assets.Add(asset);
                }
            }
            return assets;
        }

        /// <summary>Emoji by default. Without TMP's own table, the pictograph blocks are a close approximation.</summary>
        public static bool IsEmojiPresentation(uint codepoint)
        {
            return PresentationTest != null
                ? PresentationTest(codepoint)
                : codepoint >= FirstPictograph && codepoint <= LastPictograph;
        }

        /// <summary>Emoji when followed by the emoji variation selector.</summary>
        public static bool IsEmoji(uint codepoint)
        {
            return EmojiTest == null || EmojiTest(codepoint);
        }

        static Func<uint, bool> Bind(string methodName)
        {
            MethodInfo method = typeof(TMP_TextParsingUtilities).GetMethod(methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(uint) }, null);
            if (method == null || method.ReturnType != typeof(bool)) return null;
            return (Func<uint, bool>)Delegate.CreateDelegate(typeof(Func<uint, bool>), method);
        }
    }

    /// <summary>Builds what TextMeshPro searches for a character of a text object that uses a given font asset.</summary>
    internal static class TmpFontChain
    {
        const string DefaultFontAssetPath = "Fonts & Materials/";

        /// <summary>The font, its fallbacks and (optionally) the TMP Settings fallbacks and default font.</summary>
        public static List<TMP_FontAsset> Resolve(TMP_FontAsset primary, bool includeGlobalFallbacks)
        {
            if (primary == null) return new List<TMP_FontAsset>();
            return FontChainResolver.Resolve(primary, GetFallbacks, includeGlobalFallbacks ? GetGlobalFallbacks() : null);
        }

        public static List<IGlyphLookup> ToLookups(IEnumerable<TMP_FontAsset> fonts, FontFaceLoader faces = null)
        {
            faces = faces ?? new FontFaceLoader();
            var lookups = new List<IGlyphLookup>();
            foreach (TMP_FontAsset font in fonts) lookups.Add(new TmpGlyphLookup(font, faces));
            return lookups;
        }

        /// <summary>
        /// Everything TMP searches, in its order: emoji fallbacks first (emoji only), fonts named by &lt;font&gt; tags,
        /// then the font chain, and last the default sprite asset. The TMP Settings parts are left out when
        /// <paramref name="includeGlobalFallbacks"/> is false.
        /// </summary>
        public static FontChainSet Build(TMP_FontAsset primary, bool includeGlobalFallbacks)
        {
            var faces = new FontFaceLoader();
            List<IGlyphLookup> main = ToLookups(Resolve(primary, includeGlobalFallbacks), faces);
            var set = new FontChainSet
            {
                Main = main,
                FontTagResolver = name => ResolveFontTag(name, faces),
            };
            if (!includeGlobalFallbacks || TMP_Settings.instance == null) return set;

            AddSprites(main, TMP_Settings.defaultSpriteAsset);

            var emoji = new List<IGlyphLookup>();
            foreach (TMP_Asset asset in TmpEmojiSupport.FallbackAssets())
            {
                if (asset is TMP_FontAsset font) emoji.AddRange(ToLookups(Resolve(font, false), faces));
                else if (asset is TMP_SpriteAsset sprites) AddSprites(emoji, sprites);
            }
            if (emoji.Count > 0)
            {
                set.EmojiFallbacks = emoji;
                set.IsEmojiPresentation = TmpEmojiSupport.IsEmojiPresentation;
                set.IsEmoji = TmpEmojiSupport.IsEmoji;
            }
            return set;
        }

        // TMP loads the font of a <font="name"> tag from a Resources folder: TMP Settings > Default Font Asset path.
        static IReadOnlyList<IGlyphLookup> ResolveFontTag(string name, FontFaceLoader faces)
        {
            string folder = TMP_Settings.instance != null ? TMP_Settings.defaultFontAssetPath : DefaultFontAssetPath;
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(folder + name);
            return font == null ? null : ToLookups(Resolve(font, false), faces);
        }

        static void AddSprites(List<IGlyphLookup> lookups, TMP_SpriteAsset asset)
        {
            if (asset == null) return;
            foreach (TMP_SpriteAsset sprites in FontChainResolver.Resolve(asset, GetSpriteFallbacks)) lookups.Add(new TmpSpriteLookup(sprites));
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

        static IEnumerable<TMP_SpriteAsset> GetSpriteFallbacks(TMP_SpriteAsset asset)
        {
            List<TMP_SpriteAsset> fallbacks = asset.fallbackSpriteAssets;
            if (fallbacks == null) yield break;
            foreach (TMP_SpriteAsset fallback in fallbacks)
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
