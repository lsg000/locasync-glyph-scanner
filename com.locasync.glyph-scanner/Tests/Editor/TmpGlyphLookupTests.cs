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

            var result = GlyphCoverageScanner.Scan(new[] { new ScanEntry("k", "A") }, lookups);

            // 'A' is in every TMP default font; U+F8FF is a Private Use Area code point no default font maps.
            CollectionAssert.DoesNotContain(result.Missing, (uint)'A');
            CollectionAssert.Contains(result.Missing, 0xF8FFu);
        }

        [Test]
        public void Missing_font_reference_is_reported_not_thrown()
        {
            var lookup = new TmpGlyphLookup(null);

            Assert.IsFalse(lookup.Contains('A'));
            Assert.IsFalse(lookup.IsDynamic);
            Assert.AreEqual(0, TmpFontChain.Resolve(null, includeGlobalFallbacks: true).Count);
        }
    }
}
