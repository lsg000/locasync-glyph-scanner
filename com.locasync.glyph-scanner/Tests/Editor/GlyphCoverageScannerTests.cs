using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    public class FontChainResolverTests
    {
        sealed class FakeFont
        {
            public readonly string Name;
            public readonly List<FakeFont> Fallbacks = new List<FakeFont>();
            public FakeFont(string name) { Name = name; }
        }

        static List<string> Names(IEnumerable<FakeFont> fonts) => fonts.Select(f => f.Name).ToList();

        [Test]
        public void Orders_primary_then_fallbacks_depth_first_then_globals()
        {
            var a = new FakeFont("A");
            var b = new FakeFont("B");
            var c = new FakeFont("C");
            var d = new FakeFont("D");
            var e = new FakeFont("E");
            a.Fallbacks.Add(b);
            a.Fallbacks.Add(c);
            b.Fallbacks.Add(d);

            var chain = FontChainResolver.Resolve(a, f => f.Fallbacks, new[] { e });

            CollectionAssert.AreEqual(new[] { "A", "B", "D", "C", "E" }, Names(chain));
        }

        [Test]
        public void Survives_cycles_and_duplicates()
        {
            var a = new FakeFont("A");
            var b = new FakeFont("B");
            a.Fallbacks.Add(b);
            b.Fallbacks.Add(a);
            b.Fallbacks.Add(b);

            var chain = FontChainResolver.Resolve(a, f => f.Fallbacks, new[] { b, a });

            CollectionAssert.AreEqual(new[] { "A", "B" }, Names(chain));
        }

        [Test]
        public void Ignores_null_entries_and_null_fallback_lists()
        {
            var a = new FakeFont("A");
            a.Fallbacks.Add(null);

            var chain = FontChainResolver.Resolve(a, f => f.Name == "A" ? f.Fallbacks : null, new FakeFont[] { null });

            CollectionAssert.AreEqual(new[] { "A" }, Names(chain));
        }

        [Test]
        public void Null_primary_still_includes_globals()
        {
            var g = new FakeFont("G");

            var chain = FontChainResolver.Resolve(null, f => f.Fallbacks, new[] { g });

            CollectionAssert.AreEqual(new[] { "G" }, Names(chain));
        }
    }

    public class GlyphCoverageScannerTests
    {
        sealed class FakeLookup : IGlyphLookup
        {
            readonly HashSet<uint> _codepoints;
            readonly HashSet<uint> _sourceFile;

            /// <param name="characters">Characters already in the font asset.</param>
            /// <param name="sourceFileCharacters">Characters a Dynamic font can add from its source font file.</param>
            public FakeLookup(string name, string characters, string sourceFileCharacters = "", string caveat = null)
            {
                Name = name;
                Caveat = caveat;
                _codepoints = new HashSet<uint>(CodepointCollector.Enumerate(characters));
                _sourceFile = new HashSet<uint>(CodepointCollector.Enumerate(sourceFileCharacters));
            }

            public string Name { get; }
            public string Caveat { get; }
            public bool Contains(uint codepoint) => _codepoints.Contains(codepoint);
            public bool CanAddAtRuntime(uint codepoint) => _sourceFile.Contains(codepoint);
        }

        static ScanEntry[] Entries(params string[] keyTextPairs)
        {
            var entries = new ScanEntry[keyTextPairs.Length / 2];
            for (int i = 0; i < entries.Length; i++) entries[i] = new ScanEntry(keyTextPairs[i * 2], keyTextPairs[i * 2 + 1]);
            return entries;
        }

        [Test]
        public void Reports_missing_characters_sorted_and_unique()
        {
            var chain = new IGlyphLookup[] { new FakeLookup("Latin", "abc ") };

            var result = GlyphCoverageScanner.Scan(Entries("k1", "c a 나", "k2", "가 나 b"), chain);

            CollectionAssert.AreEqual(new uint[] { 0xAC00, 0xB098 }, result.Missing);
            Assert.AreEqual(2, result.EntryCount);
            Assert.AreEqual(6, result.UniqueCodepointCount); // a b c space 가 나
        }

        [Test]
        public void Fallback_fonts_cover_characters_and_are_counted()
        {
            var chain = new IGlyphLookup[] { new FakeLookup("Latin", "ab"), new FakeLookup("Korean", "가b") };

            var result = GlyphCoverageScanner.Scan(Entries("k", "ab가"), chain);

            CollectionAssert.IsEmpty(result.Missing);
            CollectionAssert.AreEqual(new[] { 2, 1 }, result.CoveredCountByChainIndex);
        }

        [Test]
        public void Remembers_up_to_three_keys_per_missing_character()
        {
            var chain = new IGlyphLookup[] { new FakeLookup("Empty", "") };

            var result = GlyphCoverageScanner.Scan(Entries("k1", "가", "k1", "가", "k2", "가", "k3", "가", "k4", "가"), chain);

            CollectionAssert.AreEqual(new[] { "k1", "k2", "k3" }, result.MissingKeys[0xAC00]);
        }

        [Test]
        public void Rich_text_tags_and_placeholders_are_not_scanned()
        {
            var chain = new IGlyphLookup[] { new FakeLookup("Latin", "Hi !") };

            var result = GlyphCoverageScanner.Scan(Entries("k", "<color=#FF0000>Hi</color> {playerName}!"), chain);

            CollectionAssert.IsEmpty(result.Missing);
        }

        [Test]
        public void Characters_a_dynamic_font_adds_at_runtime_are_not_missing()
        {
            var chain = new IGlyphLookup[] { new FakeLookup("Static", "ab"), new FakeLookup("Dynamic", "c", sourceFileCharacters: "가나") };

            var result = GlyphCoverageScanner.Scan(Entries("k", "abc가나中"), chain);

            CollectionAssert.AreEqual(new uint[] { 0x4E2D }, result.Missing);            // 中: in no atlas and no source file
            CollectionAssert.AreEqual(new uint[] { 0xAC00, 0xB098 }, result.AddedAtRuntime);
            CollectionAssert.AreEqual(new[] { 2, 3 }, result.CoveredCountByChainIndex);
            CollectionAssert.AreEqual(new[] { 0, 2 }, result.AddedAtRuntimeCountByChainIndex);
        }

        [Test]
        public void A_dynamic_font_is_asked_before_later_fonts_in_the_chain()
        {
            // TMP adds the character to the first Dynamic font that has it, even if a later font already contains it.
            var chain = new IGlyphLookup[] { new FakeLookup("Dynamic", "", sourceFileCharacters: "가"), new FakeLookup("Static", "가") };

            var result = GlyphCoverageScanner.Scan(Entries("k", "가"), chain);

            CollectionAssert.AreEqual(new[] { 1, 0 }, result.CoveredCountByChainIndex);
            CollectionAssert.AreEqual(new uint[] { 0xAC00 }, result.AddedAtRuntime);
        }

        [Test]
        public void Caveats_of_fonts_that_cannot_be_checked_are_collected_once()
        {
            var exact = GlyphCoverageScanner.Scan(Entries("k", "a"), new IGlyphLookup[] { new FakeLookup("S", "a") });
            var unsure = GlyphCoverageScanner.Scan(Entries("k", "a"), new IGlyphLookup[]
            {
                new FakeLookup("S", "a"),
                new FakeLookup("OS1", "", caveat: "uses OS fonts"),
                new FakeLookup("OS2", "", caveat: "uses OS fonts"),
            });

            CollectionAssert.IsEmpty(exact.Caveats);
            CollectionAssert.AreEqual(new[] { "uses OS fonts" }, unsure.Caveats);
        }

        [Test]
        public void Empty_chain_reports_everything_missing()
        {
            var result = GlyphCoverageScanner.Scan(Entries("k", "ab"), new IGlyphLookup[0]);

            CollectionAssert.AreEqual(new uint[] { 'a', 'b' }, result.Missing);
        }
    }

    public class CharacterSetFormatterTests
    {
        [Test]
        public void Characters_include_supplementary_planes()
        {
            Assert.AreEqual("A가😀", CharacterSetFormatter.ToCharacters(new uint[] { 'A', 0xAC00, 0x1F600 }));
        }

        [Test]
        public void Hex_ranges_merge_consecutive_code_points()
        {
            Assert.AreEqual("41-43,AC00,1F600", CharacterSetFormatter.ToHexRanges(new uint[] { 0xAC00, 'C', 'A', 'B', 'A', 0x1F600 }));
        }

        [Test]
        public void Empty_input_gives_empty_string()
        {
            Assert.AreEqual(string.Empty, CharacterSetFormatter.ToHexRanges(new uint[0]));
            Assert.AreEqual(string.Empty, CharacterSetFormatter.ToCharacters(new uint[0]));
        }

        [Test]
        public void Label_uses_at_least_four_hex_digits()
        {
            Assert.AreEqual("U+0041", CharacterSetFormatter.ToLabel('A'));
            Assert.AreEqual("U+1F600", CharacterSetFormatter.ToLabel(0x1F600));
        }
    }
}
