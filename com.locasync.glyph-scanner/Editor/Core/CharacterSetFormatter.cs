using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>Formats code points for the TMP Font Asset Creator ("Custom Characters" or "Unicode Range (Hex)").</summary>
    public static class CharacterSetFormatter
    {
        public static string ToCharacters(IEnumerable<uint> codepoints)
        {
            var sb = new StringBuilder();
            foreach (uint codepoint in codepoints)
            {
                if (!IsValidScalar(codepoint)) continue;
                sb.Append(char.ConvertFromUtf32((int)codepoint));
            }
            return sb.ToString();
        }

        /// <summary>"41-43,AC00" style: sorted, de-duplicated, consecutive code points merged into ranges.</summary>
        public static string ToHexRanges(IEnumerable<uint> codepoints)
        {
            List<uint> sorted = codepoints.Where(IsValidScalar).Distinct().OrderBy(c => c).ToList();
            var sb = new StringBuilder();

            int i = 0;
            while (i < sorted.Count)
            {
                uint start = sorted[i];
                uint end = start;
                while (i + 1 < sorted.Count && sorted[i + 1] == end + 1)
                {
                    i++;
                    end = sorted[i];
                }

                if (sb.Length > 0) sb.Append(',');
                sb.Append(start.ToString("X"));
                if (end != start) sb.Append('-').Append(end.ToString("X"));
                i++;
            }
            return sb.ToString();
        }

        public static string ToLabel(uint codepoint) => "U+" + codepoint.ToString("X4");

        static bool IsValidScalar(uint codepoint) => codepoint <= 0x10FFFF && (codepoint < 0xD800 || codepoint > 0xDFFF);
    }
}
