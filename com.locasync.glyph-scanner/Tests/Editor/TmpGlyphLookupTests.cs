using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    /// <summary>
    /// Runs against the project's real TMP default font (from TMP Settings) to verify the TMP API assumptions on the
    /// current Unity version. Skipped when the project has no TMP Settings or default font.
    /// </summary>
    public class TmpGlyphLookupTests
    {
        const uint Cyrillic = 0x0416; // Ж: not in the default font's static atlas


        static TMP_FontAsset DefaultFontOrIgnore()
        {
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                Assert.Ignore("No TMP Settings / default font asset in this project.");
            }
            return TMP_Settings.defaultFontAsset;
        }

        [Test]
        public void Chain_starts_with_the_primary_font_and_has_no_duplicates()
        {
            TMP_FontAsset font = DefaultFontOrIgnore();

            var chain = TmpFontChain.Resolve(font, includeGlobalFallbacks: true);

            Assert.AreSame(font, chain[0]);
            Assert.AreEqual(chain.Count, chain.Distinct().Count());
        }

        [Test]
        public void Latin_is_covered_and_unsupported_scripts_are_reported_missing()
        {
            TMP_FontAsset font = DefaultFontOrIgnore();
            var lookups = TmpFontChain.ToLookups(TmpFontChain.Resolve(font, includeGlobalFallbacks: false));

            var result = GlyphCoverageScanner.Scan(new[] { new ScanEntry("k", "A\uF8FF") }, lookups);

            // 'A' is in every TMP default font; U+F8FF is a Private Use Area code point no default font maps.
            CollectionAssert.DoesNotContain(result.Missing, (uint)'A');
            CollectionAssert.Contains(result.Missing, 0xF8FFu);
        }

        [Test]
        public void Missing_font_reference_is_reported_not_thrown()
        {
            var lookup = new TmpGlyphLookup(null);

            Assert.IsFalse(lookup.Contains('A'));
            Assert.IsFalse(lookup.CanAddAtRuntime('A'));
            Assert.IsNull(lookup.Caveat);
            Assert.AreEqual(0, TmpFontChain.Resolve(null, includeGlobalFallbacks: true).Count);
        }

        [Test]
        public void Static_font_never_claims_runtime_characters()
        {
            TMP_FontAsset font = DefaultFontOrIgnore();
            if (font.atlasPopulationMode != AtlasPopulationMode.Static) Assert.Ignore("The default font asset is not Static.");

            var lookup = new TmpGlyphLookup(font);

            Assert.IsNull(lookup.Caveat);
            Assert.IsFalse(lookup.CanAddAtRuntime(Cyrillic));
        }

        [Test]
        public void Dynamic_fallback_is_checked_against_its_source_font_file_without_changing_it()
        {
            // TMP Essential Resources: LiberationSans SDF (Static) falls back to a Dynamic font asset with an empty
            // atlas, whose source file LiberationSans.ttf has Cyrillic but no Hangul.
            TMP_FontAsset font = DefaultFontOrIgnore();
            List<TMP_FontAsset> chain = TmpFontChain.Resolve(font, includeGlobalFallbacks: false);
            TMP_FontAsset dynamicFont = chain.FirstOrDefault(f =>
                f.atlasPopulationMode == AtlasPopulationMode.Dynamic && f.sourceFontFile != null &&
                !f.characterLookupTable.ContainsKey(Cyrillic));
            if (dynamicFont == null) Assert.Ignore("The default font chain has no Dynamic font asset to test with.");
            int charactersBefore = dynamicFont.characterTable.Count;
            int glyphsBefore = dynamicFont.glyphTable.Count;

            var result = GlyphCoverageScanner.Scan(
                new[] { new ScanEntry("k", "AЖ가\uF8FF") }, TmpFontChain.ToLookups(chain));

            if (!result.AddedAtRuntime.Contains(Cyrillic)) Assert.Ignore("The Dynamic font's source file has no Cyrillic.");
            CollectionAssert.DoesNotContain(result.Missing, Cyrillic);
            CollectionAssert.Contains(result.Missing, 0xF8FFu);
            CollectionAssert.IsEmpty(result.Caveats);
            Assert.AreEqual(1, result.AddedAtRuntimeCountByChainIndex[chain.IndexOf(dynamicFont)]);

            // Scanning must not add anything to the Dynamic font asset.
            Assert.AreEqual(charactersBefore, dynamicFont.characterTable.Count);
            Assert.AreEqual(glyphsBefore, dynamicFont.glyphTable.Count);
            Assert.IsFalse(dynamicFont.characterLookupTable.ContainsKey(Cyrillic));
        }
    }
}
