using System.Linq;
using NUnit.Framework;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    public class RichTextStripperTests
    {
        [TestCase("<b>Hi</b>", "Hi")]
        [TestCase("<color=#FF0000>Red</color>", "Red")]
        [TestCase("<#FF0000>Red", "Red")]
        [TestCase("<size=120%>Big</size>", "Big")]
        [TestCase("<link=\"id\">Go</link>", "Go")]
        [TestCase("<sprite name=\"coin\"> 10", " 10")]
        [TestCase("<sprite=3>", "")]
        [TestCase("Line<br>Two", "LineTwo")]
        [TestCase("<B>Caps</B>", "Caps")]
        public void Removes_tmp_tags(string input, string expected)
        {
            Assert.AreEqual(expected, RichTextStripper.Strip(input));
        }

        [TestCase("a < b > c")]
        [TestCase("<foo>bar</foo>")]
        [TestCase("<#GGG>x")]
        [TestCase("1 <3")]
        [TestCase("<b")]
        public void Keeps_text_that_is_not_a_tmp_tag(string input)
        {
            Assert.AreEqual(input, RichTextStripper.Strip(input));
        }

        [Test]
        public void Keeps_noparse_content_verbatim()
        {
            Assert.AreEqual("show <b> literally", RichTextStripper.Strip("show <noparse><b></noparse> literally"));
        }

        [Test]
        public void Null_becomes_empty()
        {
            Assert.AreEqual(string.Empty, RichTextStripper.Strip(null));
        }
    }

    public class TextSanitizerTests
    {
        [TestCase("Hello {0}!", "Hello !")]
        [TestCase("Hi {playerName}", "Hi ")]
        [TestCase("{0:N2} gold", " gold")]
        [TestCase("{player.name}", "")]
        public void Strips_simple_placeholders(string input, string expected)
        {
            Assert.AreEqual(expected, TextSanitizer.StripPlaceholders(input));
        }

        [Test]
        public void Keeps_plural_forms_because_their_words_are_rendered()
        {
            const string text = "{0:plural:item|items}";
            Assert.AreEqual(text, TextSanitizer.StripPlaceholders(text));
        }

        [Test]
        public void Options_control_what_is_stripped()
        {
            const string text = "<b>{0}</b>";

            Assert.AreEqual("", TextSanitizer.Prepare(text, new ScanOptions()));
            Assert.AreEqual("{0}", TextSanitizer.Prepare(text, new ScanOptions { StripPlaceholders = false }));
            Assert.AreEqual("<b></b>", TextSanitizer.Prepare(text, new ScanOptions { StripRichTextTags = false }));
        }
    }

    public class CodepointCollectorTests
    {
        [Test]
        public void Joins_surrogate_pairs()
        {
            var codepoints = CodepointCollector.Enumerate("a😀").ToList();

            CollectionAssert.AreEqual(new uint[] { 'a', 0x1F600 }, codepoints);
        }

        [Test]
        public void Skips_characters_that_need_no_glyph()
        {
            var codepoints = CodepointCollector.Enumerate("가\n\t​‍️﻿나").ToList();

            CollectionAssert.AreEqual(new uint[] { 0xAC00, 0xB098 }, codepoints);
        }

        [Test]
        public void Skips_lone_surrogates()
        {
            var codepoints = CodepointCollector.Enumerate("x\uD83Dy").ToList();

            CollectionAssert.AreEqual(new uint[] { 'x', 'y' }, codepoints);
        }
    }
}
