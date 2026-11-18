using System.Text;
using NUnit.Framework;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    public class TextDecoderTests
    {
        const string Sample = "key,ko\nmenu,시작 🎉";

        [Test]
        public void Utf8_without_bom_is_decoded()
        {
            Assert.IsTrue(TextDecoder.TryDecode(new UTF8Encoding(false).GetBytes(Sample), out string text));
            Assert.AreEqual(Sample, text);
        }

        [Test]
        public void Utf8_bom_is_removed()
        {
            Assert.IsTrue(TextDecoder.TryDecode(WithPreamble(new UTF8Encoding(true), Sample), out string text));
            Assert.AreEqual(Sample, text);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Utf16_with_bom_is_decoded(bool bigEndian)
        {
            Assert.IsTrue(TextDecoder.TryDecode(WithPreamble(new UnicodeEncoding(bigEndian, true), Sample), out string text));
            Assert.AreEqual(Sample, text);
        }

        [Test]
        public void Legacy_encoded_bytes_are_rejected()
        {
            // "시작" as Excel's plain CSV format saves it on Korean Windows (CP949 / EUC-KR).
            byte[] cp949 = { (byte)'k', (byte)',', 0xBD, 0xC3, 0xC0, 0xDB };

            Assert.IsFalse(TextDecoder.TryDecode(cp949, out string text));
            Assert.IsEmpty(text);
        }

        [Test]
        public void Empty_input_is_empty_text()
        {
            Assert.IsTrue(TextDecoder.TryDecode(new byte[0], out string text));
            Assert.IsEmpty(text);
            Assert.IsTrue(TextDecoder.TryDecode(null, out text));
            Assert.IsEmpty(text);
        }

        [Test]
        public void Replacement_character_marks_wrongly_decoded_text()
        {
            Assert.IsTrue(TextDecoder.LooksWronglyDecoded("k,\uFFFD\uFFFD"));
            Assert.IsFalse(TextDecoder.LooksWronglyDecoded(Sample));
            Assert.IsFalse(TextDecoder.LooksWronglyDecoded(null));
        }

        static byte[] WithPreamble(Encoding encoding, string text)
        {
            byte[] preamble = encoding.GetPreamble();
            byte[] body = encoding.GetBytes(text);
            var bytes = new byte[preamble.Length + body.Length];
            preamble.CopyTo(bytes, 0);
            body.CopyTo(bytes, preamble.Length);
            return bytes;
        }
    }
}
