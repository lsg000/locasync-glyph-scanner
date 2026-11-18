using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>One font (or sprite asset) in a fallback chain, as far as the scanner is concerned.</summary>
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

    /// <summary>
    /// Everything TextMeshPro searches for a character, in three parts because not every part applies to every
    /// character: the chain of the column's font, the fonts named by &lt;font&gt; tags in the text, and the emoji
    /// fallbacks that are searched first for characters rendered as emoji.
    /// </summary>
    public sealed class FontChainSet
    {
        /// <summary>The column's font, its fallbacks and the project-wide fallbacks, in search order.</summary>
        public IReadOnlyList<IGlyphLookup> Main = new IGlyphLookup[0];

        /// <summary>
        /// Returns the font named by a &lt;font="name"&gt; tag followed by its fallbacks, or null when no such font
        /// asset exists. Text inside the tag is searched there first and then in <see cref="Main"/>.
        /// </summary>
        public Func<string, IReadOnlyList<IGlyphLookup>> FontTagResolver;

        /// <summary>Searched before everything else, but only for characters rendered as emoji.</summary>
        public IReadOnlyList<IGlyphLookup> EmojiFallbacks = new IGlyphLookup[0];

        /// <summary>True for characters that are emoji by default (unless followed by U+FE0E).</summary>
        public Func<uint, bool> IsEmojiPresentation;

        /// <summary>True for characters that become emoji when followed by U+FE0F.</summary>
        public Func<uint, bool> IsEmoji;
    }

    public sealed class ScanOptions
    {
        public bool StripRichTextTags = true;
        public bool StripPlaceholders = true;

        /// <summary>
        /// Also check the uppercase form of every letter, for text objects whose font style is UpperCase or
        /// SmallCaps (the scanner cannot see component settings).
        /// </summary>
        public bool IncludeUppercaseForms;

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

    public enum ExtraSourceKind
    {
        /// <summary>A font named by a &lt;font&gt; tag in the text, or one of its fallbacks.</summary>
        FontTag,

        /// <summary>An emoji fallback from the project settings.</summary>
        EmojiFallback,
    }

    /// <summary>A font outside the column's own chain that supplied characters.</summary>
    public sealed class ExtraSource
    {
        public string Name;
        public ExtraSourceKind Kind;
        public int CoveredCount;
        public int AddedAtRuntimeCount;
    }

    public sealed class CoverageResult
    {
        public int EntryCount;
        public int UniqueCodepointCount;

        /// <summary>Code points that cannot be rendered somewhere they are used, ascending.</summary>
        public readonly List<uint> Missing = new List<uint>();

        /// <summary>For each missing code point, the first few keys whose text uses it.</summary>
        public readonly Dictionary<uint, List<string>> MissingKeys = new Dictionary<uint, List<string>>();

        /// <summary>
        /// Code points that are in no atlas yet but that a Dynamic font adds at runtime, ascending.
        /// They render, so they are not in <see cref="Missing"/>.
        /// </summary>
        public readonly List<uint> AddedAtRuntime = new List<uint>();

        /// <summary>How many unique code points each entry of the main chain supplied (same order as the chain).</summary>
        public int[] CoveredCountByChainIndex = new int[0];

        /// <summary>The part of <see cref="CoveredCountByChainIndex"/> that the font adds at runtime.</summary>
        public int[] AddedAtRuntimeCountByChainIndex = new int[0];

        /// <summary>Fonts from &lt;font&gt; tags and emoji fallbacks that supplied at least one character.</summary>
        public readonly List<ExtraSource> ExtraSources = new List<ExtraSource>();

        /// <summary>
        /// Fonts that could not be fully checked, so characters listed as missing may still render.
        /// Empty when the result is exact.
        /// </summary>
        public readonly List<string> Caveats = new List<string>();

        /// <summary>Problems in the text itself that are worth fixing even when nothing is missing.</summary>
        public readonly List<string> Warnings = new List<string>();
    }

    public static class GlyphCoverageScanner
    {
        /// <summary>A place where characters are used: inside a font tag or not, rendered as emoji or not.</summary>
        sealed class Usage
        {
            public string FontName;
            public bool AsEmoji;
            public readonly SortedSet<uint> Codepoints = new SortedSet<uint>();
        }

        sealed class Tally
        {
            public readonly HashSet<uint> Covered = new HashSet<uint>();
            public readonly HashSet<uint> AddedAtRuntime = new HashSet<uint>();
        }

        sealed class ReferenceComparer : IEqualityComparer<IGlyphLookup>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(IGlyphLookup x, IGlyphLookup y) => ReferenceEquals(x, y);
            public int GetHashCode(IGlyphLookup obj) => RuntimeHelpers.GetHashCode(obj);
        }

        /// <summary>Scans against one chain: no &lt;font&gt; tag fonts and no emoji fallbacks.</summary>
        public static CoverageResult Scan(IEnumerable<ScanEntry> entries, IReadOnlyList<IGlyphLookup> chain, ScanOptions options = null)
        {
            return Scan(entries, new FontChainSet { Main = chain }, options);
        }

        public static CoverageResult Scan(IEnumerable<ScanEntry> entries, FontChainSet chains, ScanOptions options = null)
        {
            options = options ?? new ScanOptions();
            IReadOnlyList<IGlyphLookup> main = chains.Main ?? new IGlyphLookup[0];
            IReadOnlyList<IGlyphLookup> emojiFallbacks = chains.EmojiFallbacks ?? new IGlyphLookup[0];

            // 1. Collect the characters, remembering where each one is used.
            var used = new SortedSet<uint>();
            var keysByCodepoint = new Dictionary<uint, List<string>>();
            var usages = new Dictionary<string, Usage>();
            var usageOrder = new List<Usage>();
            int entryCount = 0;

            foreach (ScanEntry entry in entries)
            {
                entryCount++;
                foreach (TextRun run in TextSanitizer.PrepareRuns(entry.Text, options))
                {
                    string fontName = chains.FontTagResolver != null ? run.FontName : null;
                    foreach (StyledCodepoint styled in CodepointCollector.EnumerateStyled(run.Text))
                    {
                        uint codepoint = styled.Codepoint;
                        if (used.Add(codepoint)) keysByCodepoint[codepoint] = new List<string>();

                        List<string> keys = keysByCodepoint[codepoint];
                        if (keys.Count < options.MaxKeysPerCharacter && !keys.Contains(entry.Key)) keys.Add(entry.Key);

                        bool asEmoji = emojiFallbacks.Count > 0 && IsRenderedAsEmoji(styled, chains);
                        string usageKey = (asEmoji ? "E:" : "T:") + fontName;
                        if (!usages.TryGetValue(usageKey, out Usage usage))
                        {
                            usage = new Usage { FontName = fontName, AsEmoji = asEmoji };
                            usages[usageKey] = usage;
                            usageOrder.Add(usage);
                        }
                        usage.Codepoints.Add(codepoint);
                    }
                }
            }

            var result = new CoverageResult
            {
                EntryCount = entryCount,
                UniqueCodepointCount = used.Count,
                CoveredCountByChainIndex = new int[main.Count],
                AddedAtRuntimeCountByChainIndex = new int[main.Count],
            };

            // 2. Check each usage against the fonts TextMeshPro searches for it.
            var tallies = new Dictionary<IGlyphLookup, Tally>(ReferenceComparer.Instance);
            var tagChains = new Dictionary<string, IReadOnlyList<IGlyphLookup>>();
            var missing = new SortedSet<uint>();
            var addedAtRuntime = new SortedSet<uint>();

            foreach (Usage usage in usageOrder)
            {
                var chain = new List<IGlyphLookup>();
                if (usage.AsEmoji) chain.AddRange(emojiFallbacks);
                if (usage.FontName != null)
                {
                    if (!tagChains.TryGetValue(usage.FontName, out IReadOnlyList<IGlyphLookup> tagChain))
                    {
                        tagChain = chains.FontTagResolver(usage.FontName);
                        tagChains[usage.FontName] = tagChain;
                        if (tagChain == null)
                        {
                            result.Warnings.Add("The font asset named by <font=\"" + usage.FontName + "\"> was not found. " +
                                                "TextMeshPro shows such a tag as plain text. The text inside it was " +
                                                "checked against the column's font.");
                        }
                    }
                    if (tagChain != null) chain.AddRange(tagChain);
                }
                chain.AddRange(main);

                foreach (uint codepoint in usage.Codepoints)
                {
                    // TextMeshPro asks each font in turn: is the character in the asset, and if not, can this
                    // (Dynamic) font add it from its source file? Only then does it move on to the next font.
                    IGlyphLookup coveredBy = null;
                    bool runtime = false;
                    foreach (IGlyphLookup lookup in chain)
                    {
                        if (lookup.Contains(codepoint))
                        {
                            coveredBy = lookup;
                            break;
                        }
                        if (lookup.CanAddAtRuntime(codepoint))
                        {
                            coveredBy = lookup;
                            runtime = true;
                            break;
                        }
                    }

                    if (coveredBy == null)
                    {
                        missing.Add(codepoint);
                        continue;
                    }

                    if (!tallies.TryGetValue(coveredBy, out Tally tally))
                    {
                        tally = new Tally();
                        tallies[coveredBy] = tally;
                    }
                    tally.Covered.Add(codepoint);
                    if (runtime)
                    {
                        tally.AddedAtRuntime.Add(codepoint);
                        addedAtRuntime.Add(codepoint);
                    }
                }
            }

            // 3. Report.
            foreach (uint codepoint in missing)
            {
                result.Missing.Add(codepoint);
                result.MissingKeys[codepoint] = keysByCodepoint[codepoint];
            }
            result.AddedAtRuntime.AddRange(addedAtRuntime);

            for (int i = 0; i < main.Count; i++)
            {
                AddCaveat(result, main[i].Caveat);
                if (!tallies.TryGetValue(main[i], out Tally tally)) continue;
                result.CoveredCountByChainIndex[i] = tally.Covered.Count;
                result.AddedAtRuntimeCountByChainIndex[i] = tally.AddedAtRuntime.Count;
            }

            AddExtraSources(result, emojiFallbacks, ExtraSourceKind.EmojiFallback, tallies);
            foreach (IReadOnlyList<IGlyphLookup> tagChain in tagChains.Values)
            {
                if (tagChain != null) AddExtraSources(result, tagChain, ExtraSourceKind.FontTag, tallies);
            }
            return result;
        }

        // Same test as TextMeshPro: an emoji-by-default character unless a text selector follows it, or any emoji
        // character that is followed by the emoji selector.
        static bool IsRenderedAsEmoji(StyledCodepoint styled, FontChainSet chains)
        {
            if (styled.Hint != VariationHint.Text && chains.IsEmojiPresentation != null && chains.IsEmojiPresentation(styled.Codepoint))
                return true;
            return styled.Hint == VariationHint.Emoji && chains.IsEmoji != null && chains.IsEmoji(styled.Codepoint);
        }

        static void AddExtraSources(CoverageResult result, IReadOnlyList<IGlyphLookup> lookups, ExtraSourceKind kind,
            Dictionary<IGlyphLookup, Tally> tallies)
        {
            foreach (IGlyphLookup lookup in lookups)
            {
                AddCaveat(result, lookup.Caveat);
                if (!tallies.TryGetValue(lookup, out Tally tally)) continue;
                result.ExtraSources.Add(new ExtraSource
                {
                    Name = lookup.Name,
                    Kind = kind,
                    CoveredCount = tally.Covered.Count,
                    AddedAtRuntimeCount = tally.AddedAtRuntime.Count,
                });
            }
        }

        static void AddCaveat(CoverageResult result, string caveat)
        {
            if (!string.IsNullOrEmpty(caveat) && !result.Caveats.Contains(caveat)) result.Caveats.Add(caveat);
        }
    }
}
