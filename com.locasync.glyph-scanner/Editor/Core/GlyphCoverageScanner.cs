using System.Collections.Generic;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>One font in a fallback chain, as far as the scanner is concerned.</summary>
    public interface IGlyphLookup
    {
        string Name { get; }

        /// <summary>True when the font can add glyphs from its source file at runtime (TMP Dynamic atlas).</summary>
        bool IsDynamic { get; }

        bool Contains(uint codepoint);
    }

    public sealed class ScanOptions
    {
        public bool StripRichTextTags = true;
        public bool StripPlaceholders = true;
        public int MaxKeysPerCharacter = 3;
    }

    public readonly struct ScanEntry
    {
        public readonly string Key;
        public readonly string Text;

        public ScanEntry(string key, string text)
        {
            Key = key;
            Text = text;
        }
    }

    public sealed class CoverageResult
    {
        public int EntryCount;
        public int UniqueCodepointCount;

        /// <summary>Missing code points, ascending.</summary>
        public readonly List<uint> Missing = new List<uint>();

        /// <summary>For each missing code point, the first few keys whose text uses it.</summary>
        public readonly Dictionary<uint, List<string>> MissingKeys = new Dictionary<uint, List<string>>();

        /// <summary>How many unique code points each chain entry supplied (same order as the chain).</summary>
        public int[] CoveredCountByChainIndex = new int[0];

        public bool ChainHasDynamicFont;
    }

    public static class GlyphCoverageScanner
    {
        public static CoverageResult Scan(IEnumerable<ScanEntry> entries, IReadOnlyList<IGlyphLookup> chain, ScanOptions options = null)
        {
            options = options ?? new ScanOptions();

            var used = new SortedSet<uint>();
            var keysByCodepoint = new Dictionary<uint, List<string>>();
            int entryCount = 0;

            foreach (ScanEntry entry in entries)
            {
                entryCount++;
                string text = TextSanitizer.Prepare(entry.Text, options);
                foreach (uint codepoint in CodepointCollector.Enumerate(text))
                {
                    if (used.Add(codepoint)) keysByCodepoint[codepoint] = new List<string>();

                    List<string> keys = keysByCodepoint[codepoint];
                    if (keys.Count < options.MaxKeysPerCharacter && !keys.Contains(entry.Key)) keys.Add(entry.Key);
                }
            }

            var result = new CoverageResult
            {
                EntryCount = entryCount,
                UniqueCodepointCount = used.Count,
                CoveredCountByChainIndex = new int[chain.Count],
            };
            for (int i = 0; i < chain.Count; i++)
            {
                if (chain[i].IsDynamic) result.ChainHasDynamicFont = true;
            }

            foreach (uint codepoint in used)
            {
                int coveredBy = -1;
                for (int i = 0; i < chain.Count; i++)
                {
                    if (chain[i].Contains(codepoint))
                    {
                        coveredBy = i;
                        break;
                    }
                }

                if (coveredBy >= 0)
                {
                    result.CoveredCountByChainIndex[coveredBy]++;
                }
                else
                {
                    result.Missing.Add(codepoint);
                    result.MissingKeys[codepoint] = keysByCodepoint[codepoint];
                }
            }
            return result;
        }
    }
}
