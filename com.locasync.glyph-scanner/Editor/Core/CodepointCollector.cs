using System.Collections.Generic;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Enumerates Unicode code points (UTF-32), joining surrogate pairs so emoji and CJK Extension B+
    /// characters are treated as single characters. Characters that never need a glyph are skipped.
    /// </summary>
    public static class CodepointCollector
    {
        public static IEnumerable<uint> Enumerate(string text)
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

                if (!IsIgnored(codepoint)) yield return codepoint;
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
