using System.Collections.Generic;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>What a variation selector right after a character asks for.</summary>
    public enum VariationHint
    {
        None,

        /// <summary>Followed by U+FE0F: render as a colour emoji.</summary>
        Emoji,

        /// <summary>Followed by U+FE0E: render as plain text.</summary>
        Text,
    }

    public readonly struct StyledCodepoint
    {
        public readonly uint Codepoint;
        public readonly VariationHint Hint;

        public StyledCodepoint(uint codepoint, VariationHint hint)
        {
            Codepoint = codepoint;
            Hint = hint;
        }
    }

    /// <summary>
    /// Enumerates Unicode code points (UTF-32), joining surrogate pairs so emoji and CJK Extension B+
    /// characters are treated as single characters. Characters that never need a glyph are skipped.
    /// </summary>
    public static class CodepointCollector
    {
        const char EmojiSelector = (char)0xFE0F;
        const char TextSelector = (char)0xFE0E;

        public static IEnumerable<uint> Enumerate(string text)
        {
            foreach (StyledCodepoint styled in EnumerateStyled(text)) yield return styled.Codepoint;
        }

        /// <summary>Like <see cref="Enumerate"/>, and also says whether a variation selector follows each character.</summary>
        public static IEnumerable<StyledCodepoint> EnumerateStyled(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                uint codepoint;
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codepoint = (uint)char.ConvertToUtf32(c, text[i + 1]);
                    i++;
                }
                else
                {
                    codepoint = c;
                }

                if (IsIgnored(codepoint)) continue;

                var hint = VariationHint.None;
                if (i + 1 < text.Length)
                {
                    if (text[i + 1] == EmojiSelector) hint = VariationHint.Emoji;
                    else if (text[i + 1] == TextSelector) hint = VariationHint.Text;
                }
                yield return new StyledCodepoint(codepoint, hint);
            }
        }

        /// <summary>Control characters, zero-width joiners/spaces, BOM, variation selectors and lone surrogates.</summary>
        public static bool IsIgnored(uint codepoint)
        {
            if (codepoint < 0x20) return true;
            if (codepoint >= 0x7F && codepoint <= 0x9F) return true;
            if (codepoint >= 0xD800 && codepoint <= 0xDFFF) return true;
            if (codepoint >= 0x200B && codepoint <= 0x200D) return true;
            if (codepoint == 0x2060 || codepoint == 0xFEFF) return true;
            if (codepoint >= 0xFE00 && codepoint <= 0xFE0F) return true;
            return false;
        }
    }
}
