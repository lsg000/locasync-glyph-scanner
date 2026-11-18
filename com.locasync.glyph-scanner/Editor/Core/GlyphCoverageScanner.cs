using System.Collections.Generic;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>One font in a fallback chain, as far as the scanner is concerned.</summary>
    public interface IGlyphLookup
    {
        string Name { get; }

        /// <summary>
        /// Non-null when the font may render characters the scanner cannot verify in the editor (for example a
        /// Dynamic font whose source font file cannot be read). The text explains why.
        /// </summary>
        string Caveat { get; }

        /// <summary>True when the character is already in the font asset (its atlas has the glyph).</summary>
        bool Contains(uint codepoint);

        /// <summary>
        /// True when the character is not in the font asset yet but the font adds it at runtime, as a TMP Dynamic
        /// font does when its source font file contains the glyph. Only asked when <see cref="Contains"/> is false.
        /// </summary>
        bool CanAddAtRuntime(uint codepoint);
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

        /// <summary>Code points no font in the chain can render, ascending.</summary>
        public readonly List<uint> Missing = new List<uint>();

        /// <summary>For each missing code point, the first few keys whose text uses it.</summary>
        public readonly Dictionary<uint, List<string>> MissingKeys = new Dictionary<uint, List<string>>();

        /// <summary>
        /// Code points that are in no atlas yet but that a Dynamic font in the chain adds at runtime, ascending.
        /// They render, so they are not in <see cref="Missing"/>.
        /// </summary>
        public readonly List<uint> AddedAtRuntime = new List<uint>();

        /// <summary>How many unique code points each chain entry supplied (same order as the chain).</summary>
        public int[] CoveredCountByChainIndex = new int[0];

        /// <summary>The part of <see cref="CoveredCountByChainIndex"/> that the font adds at runtime.</summary>
        public int[] AddedAtRuntimeCountByChainIndex = new int[0];

        /// <summary>Reasons why some fonts in the chain could not be fully checked. Empty when the result is exact.</summary>
        public readonly List<string> Caveats = new List<string>();
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
                AddedAtRuntimeCountByChainIndex = new int[chain.Count],
            };
            for (int i = 0; i < chain.Count; i++)
            {
                string caveat = chain[i].Caveat;
                if (!string.IsNullOrEmpty(caveat) && !result.Caveats.Contains(caveat)) result.Caveats.Add(caveat);
            }

            foreach (uint codepoint in used)
            {
                // TextMeshPro asks each font in turn: is the character in the asset, and if not, can this (Dynamic)
                // font add it from its source file? Only then does it move on to the next font in the chain.
                int coveredBy = -1;
                bool addedAtRuntime = false;
                for (int i = 0; i < chain.Count; i++)
                {
                    if (chain[i].Contains(codepoint))
                    {
                        coveredBy = i;
                        break;
                    }
                    if (chain[i].CanAddAtRuntime(codepoint))
                    {
                        coveredBy = i;
                        addedAtRuntime = true;
                        break;
                    }
                }

                if (coveredBy < 0)
                {
                    result.Missing.Add(codepoint);
                    result.MissingKeys[codepoint] = keysByCodepoint[codepoint];
                    continue;
                }

                result.CoveredCountByChainIndex[coveredBy]++;
                if (addedAtRuntime)
                {
                    result.AddedAtRuntimeCountByChainIndex[coveredBy]++;
                    result.AddedAtRuntime.Add(codepoint);
                }
            }
            return result;
        }
    }
}
