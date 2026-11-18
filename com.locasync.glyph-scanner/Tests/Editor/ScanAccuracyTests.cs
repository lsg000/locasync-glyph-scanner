using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    /// <summary>Rich text that changes which glyphs are needed: case tags and font tags.</summary>
    public class RichTextRunTests
    {
        [TestCase("<allcaps>abc é</allcaps> x", "ABC É x")]
        [TestCase("<uppercase>abc</uppercase>", "ABC")]
        [TestCase("<lowercase>ABC É</lowercase>", "abc é")]
        [TestCase("<smallcaps>aB</smallcaps>", "AB")]
        [TestCase("<allcaps>a</allcaps>b", "Ab")]
        [TestCase("<lowercase><allcaps>aB</allcaps>aB</lowercase>", "ABab")]
        [TestCase("<allcaps>1 가 !</allcaps>", "1 가 !")]
        [TestCase("<allcaps><noparse><b>x</noparse></allcaps>", "<B>X")]
        [TestCase("</allcaps>a", "a")]
        public void Case_tags_change_the_letters(string input, string expected)
        {
            Assert.AreEqual(expected, RichTextStripper.Strip(input));
        }

        [Test]
        public void Text_without_tags_is_one_run_in_the_objects_own_font()
        {
            List<TextRun> runs = RichTextStripper.Parse("plain");

            Assert.AreEqual(1, runs.Count);
            Assert.AreEqual("plain", runs[0].Text);
            Assert.IsNull(runs[0].FontName);
        }

        [TestCase("A<font=\"Icons SDF\">B</font>C")]
        [TestCase("A<font=\"Icons SDF\" material=\"Icons Outline\">B</font>C")]
        [TestCase("A<FONT=\"Icons SDF\">B</FONT>C")]
        public void Font_tags_split_the_text_into_runs(string input)
        {
            List<TextRun> runs = RichTextStripper.Parse(input);

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, runs.Select(r => r.Text).ToList());
            CollectionAssert.AreEqual(new[] { null, "Icons SDF", null }, runs.Select(r => r.FontName).ToList());
        }

        [Test]
        public void Font_name_may_be_written_without_quotes()
        {
            List<TextRun> runs = RichTextStripper.Parse("<font=Icons>x</font>");

            Assert.AreEqual("Icons", runs[0].FontName);
        }

        [Test]
        public void Nested_font_tags_return_to_the_enclosing_font()
        {
            List<TextRun> runs = RichTextStripper.Parse("<font=\"One\">a<font=\"Two\">b</font>c</font>d");

            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, runs.Select(r => r.Text).ToList());
            CollectionAssert.AreEqual(new[] { "One", "Two", "One", null }, runs.Select(r => r.FontName).ToList());
        }

        [TestCase("<font=\"default\">x</font>")]
        [TestCase("<font=Default>x</font>")]
        public void Default_font_tag_means_the_objects_own_font(string input)
        {
            List<TextRun> runs = RichTextStripper.Parse(input);

            Assert.AreEqual(1, runs.Count);
            Assert.IsNull(runs[0].FontName);
        }

        [Test]
        public void Other_tags_inside_a_font_tag_are_still_removed()
        {
            List<TextRun> runs = RichTextStripper.Parse("<font=\"Icons\"><b>x</b><allcaps>y</allcaps></font>");

            Assert.AreEqual(1, runs.Count);
            Assert.AreEqual("xY", runs[0].Text);
            Assert.AreEqual("Icons", runs[0].FontName);
        }

        [Test]
        public void Text_made_only_of_tags_has_no_runs()
        {
            CollectionAssert.IsEmpty(RichTextStripper.Parse("<b></b><font=\"Icons\"></font>"));
            Assert.AreEqual(string.Empty, RichTextStripper.Strip("<b></b>"));
        }
    }

    public class StyledCodepointTests
    {
        static readonly string Heart = char.ConvertFromUtf32(0x2764);
        static readonly string Party = char.ConvertFromUtf32(0x1F389);
        const char EmojiSelector = (char)0xFE0F;
        const char TextSelector = (char)0xFE0E;

        [Test]
        public void Variation_selectors_are_reported_as_hints_not_characters()
        {
            List<StyledCodepoint> result = CodepointCollector
                .EnumerateStyled("a" + Heart + EmojiSelector + Heart + TextSelector + Party + EmojiSelector + Heart)
                .ToList();

            CollectionAssert.AreEqual(new uint[] { 'a', 0x2764, 0x2764, 0x1F389, 0x2764 }, result.Select(s => s.Codepoint).ToList());
            CollectionAssert.AreEqual(
                new[] { VariationHint.None, VariationHint.Emoji, VariationHint.Text, VariationHint.Emoji, VariationHint.None },
                result.Select(s => s.Hint).ToList());
        }
    }

    /// <summary>Scans that involve more than the column's own font chain.</summary>
    public class FontChainSetScanTests
    {
        const uint Star = 0x2605, Heart = 0x2764, Party = 0x1F389, CapitalEAcute = 0xC9;
        const char EmojiSelector = (char)0xFE0F;
        const char TextSelector = (char)0xFE0E;

        static readonly string StarText = char.ConvertFromUtf32((int)Star);
        static readonly string HeartText = char.ConvertFromUtf32((int)Heart);
        static readonly string PartyText = char.ConvertFromUtf32((int)Party);

        sealed class FakeLookup : IGlyphLookup
        {
            readonly HashSet<uint> _codepoints;
            readonly HashSet<uint> _sourceFile;

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

        static ScanEntry[] Entry(string text) => new[] { new ScanEntry("k", text) };

        static FontChainSet ChainsWithIconFont(string mainCharacters = "ab")
        {
            var icons = new IGlyphLookup[] { new FakeLookup("Icons", StarText), new FakeLookup("Icons Fallback", "", sourceFileCharacters: "z") };
            return new FontChainSet
            {
                Main = new IGlyphLookup[] { new FakeLookup("Main", mainCharacters) },
                FontTagResolver = name => name == "Icons" ? icons : null,
            };
        }

        // ---------------------------------------------------------------- <font> tags

        [Test]
        public void Text_inside_a_font_tag_is_checked_against_that_font_first()
        {
            CoverageResult result = GlyphCoverageScanner.Scan(Entry("a<font=\"Icons\">" + StarText + "z</font>b"), ChainsWithIconFont());

            CollectionAssert.IsEmpty(result.Missing);
            CollectionAssert.IsEmpty(result.Caveats);
            CollectionAssert.AreEqual(new[] { 2 }, result.CoveredCountByChainIndex);
            CollectionAssert.AreEqual(new[] { "Icons", "Icons Fallback" }, result.ExtraSources.Select(s => s.Name).ToList());
            Assert.IsTrue(result.ExtraSources.All(s => s.Kind == ExtraSourceKind.FontTag));
            Assert.AreEqual(1, result.ExtraSources[1].AddedAtRuntimeCount);
            CollectionAssert.AreEqual(new uint[] { 'z' }, result.AddedAtRuntime);
        }

        [Test]
        public void Text_inside_a_font_tag_still_falls_back_to_the_columns_chain()
        {
            CoverageResult result = GlyphCoverageScanner.Scan(Entry("<font=\"Icons\">a</font>"), ChainsWithIconFont());

            CollectionAssert.IsEmpty(result.Missing);
            CollectionAssert.AreEqual(new[] { 1 }, result.CoveredCountByChainIndex);
            CollectionAssert.IsEmpty(result.ExtraSources);
        }

        [Test]
        public void A_character_is_missing_if_it_is_missing_anywhere_it_is_used()
        {
            CoverageResult result = GlyphCoverageScanner.Scan(Entry(StarText + "<font=\"Icons\">" + StarText + "</font>"), ChainsWithIconFont());

            CollectionAssert.AreEqual(new[] { Star }, result.Missing);
            Assert.AreEqual(1, result.UniqueCodepointCount);
        }

        [Test]
        public void Unknown_font_tag_is_checked_against_the_column_font_and_reported()
        {
            CoverageResult result = GlyphCoverageScanner.Scan(Entry("<font=\"Nope\">a" + StarText + "</font>"), ChainsWithIconFont());

            CollectionAssert.AreEqual(new[] { Star }, result.Missing);
            Assert.AreEqual(1, result.Warnings.Count);
            StringAssert.Contains("Nope", result.Warnings[0]);
            CollectionAssert.IsEmpty(result.Caveats);
        }

        [Test]
        public void Font_tags_are_ignored_when_the_chain_cannot_resolve_them()
        {
            var plainChain = new IGlyphLookup[] { new FakeLookup("Main", "ab") };

            CoverageResult result = GlyphCoverageScanner.Scan(Entry("<font=\"Icons\">a" + StarText + "</font>"), plainChain);

            CollectionAssert.AreEqual(new[] { Star }, result.Missing);
            CollectionAssert.IsEmpty(result.Caveats);
            CollectionAssert.IsEmpty(result.Warnings);
        }

        [Test]
        public void A_wrong_font_name_is_reported_once_however_often_it_is_used()
        {
            var entries = new[] { new ScanEntry("k1", "<font=\"Nope\">a</font>"), new ScanEntry("k2", "<font=\"Nope\">b</font>") };

            CoverageResult result = GlyphCoverageScanner.Scan(entries, ChainsWithIconFont());

            CollectionAssert.IsEmpty(result.Missing);
            Assert.AreEqual(1, result.Warnings.Count);
        }

        // ---------------------------------------------------------------- emoji fallbacks

        static FontChainSet ChainsWithEmojiFallback()
        {
            return new FontChainSet
            {
                Main = new IGlyphLookup[] { new FakeLookup("Main", "ab") },
                EmojiFallbacks = new IGlyphLookup[] { new FakeLookup("Emoji", HeartText + PartyText + "a") },
                IsEmojiPresentation = c => c == Party,
                IsEmoji = c => c == Party || c == Heart,
            };
        }

        [Test]
        public void Emoji_by_default_are_found_in_the_emoji_fallbacks()
        {
            CoverageResult result = GlyphCoverageScanner.Scan(Entry("a" + PartyText), ChainsWithEmojiFallback());

            CollectionAssert.IsEmpty(result.Missing);
            Assert.AreEqual(1, result.ExtraSources.Count);
            Assert.AreEqual("Emoji", result.ExtraSources[0].Name);
            Assert.AreEqual(ExtraSourceKind.EmojiFallback, result.ExtraSources[0].Kind);
            Assert.AreEqual(1, result.ExtraSources[0].CoveredCount); // 'a' is not an emoji, so the main font supplies it
        }

        [Test]
        public void Text_style_emoji_need_the_emoji_selector_to_use_the_emoji_fallbacks()
        {
            CoverageResult bare = GlyphCoverageScanner.Scan(Entry(HeartText), ChainsWithEmojiFallback());
            CoverageResult withSelector = GlyphCoverageScanner.Scan(Entry(HeartText + EmojiSelector), ChainsWithEmojiFallback());

            CollectionAssert.AreEqual(new[] { Heart }, bare.Missing);
            CollectionAssert.IsEmpty(withSelector.Missing);
        }

        [Test]
        public void Text_selector_keeps_an_emoji_out_of_the_emoji_fallbacks()
        {
            CoverageResult result = GlyphCoverageScanner.Scan(Entry(PartyText + TextSelector), ChainsWithEmojiFallback());

            CollectionAssert.AreEqual(new[] { Party }, result.Missing);
        }

        // ---------------------------------------------------------------- uppercase

        [Test]
        public void Allcaps_tag_needs_the_uppercase_glyph()
        {
            var chain = new IGlyphLookup[] { new FakeLookup("Main", "é") };

            CoverageResult plain = GlyphCoverageScanner.Scan(Entry("é"), chain);
            CoverageResult caps = GlyphCoverageScanner.Scan(Entry("<allcaps>é</allcaps>"), chain);

            CollectionAssert.IsEmpty(plain.Missing);
            CollectionAssert.AreEqual(new[] { CapitalEAcute }, caps.Missing);
        }

        [Test]
        public void Uppercase_option_checks_both_forms_of_every_letter()
        {
            var chain = new IGlyphLookup[] { new FakeLookup("Main", "éA") };

            CoverageResult result = GlyphCoverageScanner.Scan(Entry("éA 1"), chain, new ScanOptions { IncludeUppercaseForms = true });

            CollectionAssert.AreEqual(new uint[] { ' ', '1', CapitalEAcute }, result.Missing);
        }
    }
}
