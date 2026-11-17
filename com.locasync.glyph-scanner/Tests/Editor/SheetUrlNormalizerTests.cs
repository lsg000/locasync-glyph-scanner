using NUnit.Framework;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    public class SheetUrlNormalizerTests
    {
        const string Id = "1AbC-dEf_123";
        const string PublishedId = "2PACX-1vTxyz";

        [TestCase("https://docs.google.com/spreadsheets/d/" + Id + "/edit#gid=123",
            "https://docs.google.com/spreadsheets/d/" + Id + "/export?format=csv&gid=123")]
        [TestCase("https://docs.google.com/spreadsheets/d/" + Id + "/edit?usp=sharing",
            "https://docs.google.com/spreadsheets/d/" + Id + "/export?format=csv&gid=0")]
        [TestCase("  https://docs.google.com/spreadsheets/d/" + Id + "/edit?gid=7#gid=7  ",
            "https://docs.google.com/spreadsheets/d/" + Id + "/export?format=csv&gid=7")]
        public void Shared_links_become_export_links(string input, string expected)
        {
            Assert.IsTrue(SheetUrlNormalizer.TryNormalize(input, out string url, out _));
            Assert.AreEqual(expected, url);
        }

        [TestCase("https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pubhtml",
            "https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pub?output=csv")]
        [TestCase("https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pubhtml?gid=42&single=true",
            "https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pub?gid=42&single=true&output=csv")]
        [TestCase("https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pub?gid=42&single=true&output=tsv",
            "https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pub?gid=42&single=true&output=tsv")]
        [TestCase("https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pubhtml/sheet?headers=false&gid=5",
            "https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/pub?gid=5&single=true&output=csv")]
        public void Published_links_become_csv_or_tsv_links(string input, string expected)
        {
            Assert.IsTrue(SheetUrlNormalizer.TryNormalize(input, out string url, out _));
            Assert.AreEqual(expected, url);
        }

        [Test]
        public void Other_urls_pass_through()
        {
            const string input = "https://example.com/strings.csv";

            Assert.IsTrue(SheetUrlNormalizer.TryNormalize(input, out string url, out _));
            Assert.AreEqual(input, url);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not a url")]
        [TestCase("ftp://example.com/strings.csv")]
        [TestCase("https://docs.google.com/spreadsheets/d/e/" + PublishedId + "/edit")]
        public void Rejects_unusable_input_with_a_message(string input)
        {
            Assert.IsFalse(SheetUrlNormalizer.TryNormalize(input, out _, out string error));
            Assert.IsNotEmpty(error);
        }
    }
}
