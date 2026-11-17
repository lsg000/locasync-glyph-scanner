using NUnit.Framework;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    public class DelimitedTextParserTests
    {
        [Test]
        public void Parses_simple_rows()
        {
            var rows = DelimitedTextParser.Parse("key,en\nhello,Hello\n", ',');

            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "key", "en" }, rows[0]);
            CollectionAssert.AreEqual(new[] { "hello", "Hello" }, rows[1]);
        }

        [Test]
        public void Quoted_field_keeps_delimiters_and_line_breaks()
        {
            var rows = DelimitedTextParser.Parse("k,v\na,\"one, two\nthree\"\n", ',');

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("one, two\nthree", rows[1][1]);
        }

        [Test]
        public void Doubled_quotes_become_one_quote()
        {
            var rows = DelimitedTextParser.Parse("a,\"say \"\"hi\"\"\"", ',');

            Assert.AreEqual("say \"hi\"", rows[0][1]);
        }

        [Test]
        public void Handles_crlf_and_bom()
        {
            var rows = DelimitedTextParser.Parse("﻿key,en\r\nx,y\r\n", ',');

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("key", rows[0][0]);
            Assert.AreEqual("y", rows[1][1]);
        }

        [Test]
        public void Last_row_without_newline_is_kept()
        {
            var rows = DelimitedTextParser.Parse("a,b\nc,d", ',');

            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(new[] { "c", "d" }, rows[1]);
        }

        [Test]
        public void Empty_trailing_field_is_kept()
        {
            var rows = DelimitedTextParser.Parse("a,b,\n", ',');

            CollectionAssert.AreEqual(new[] { "a", "b", "" }, rows[0]);
        }

        [TestCase("key\ten\tko\nx\ty\tz", '\t')]
        [TestCase("key,en,ko\nx,y,z", ',')]
        [TestCase("key;en;ko\nx;y;z", ';')]
        [TestCase("\"a,b\"\tc\n", '\t')]
        [TestCase("", ',')]
        public void Detects_delimiter_from_first_line(string text, char expected)
        {
            Assert.AreEqual(expected, DelimitedTextParser.DetectDelimiter(text));
        }
    }

    public class TextTableTests
    {
        [Test]
        public void First_non_blank_row_is_header_and_rows_are_padded()
        {
            var rows = DelimitedTextParser.Parse("\n , \nkey,en,ko\nhello,Hello\n\n", ',');
            var table = TextTable.FromRows(rows);

            CollectionAssert.AreEqual(new[] { "key", "en", "ko" }, table.Headers);
            Assert.AreEqual(1, table.Rows.Count);
            CollectionAssert.AreEqual(new[] { "hello", "Hello", "" }, table.Rows[0]);
        }

        [Test]
        public void IndexOf_is_case_insensitive_and_trimmed()
        {
            var table = TextTable.FromRows(DelimitedTextParser.Parse(" Key , English(en) ,ko", ','));

            Assert.AreEqual(0, table.IndexOf("key"));
            Assert.AreEqual(1, table.IndexOf("english(EN)"));
            Assert.AreEqual(-1, table.IndexOf("ja"));
            Assert.AreEqual(-1, table.IndexOf(""));
        }

        [Test]
        public void Returns_null_when_there_is_no_header()
        {
            Assert.IsNull(TextTable.FromRows(DelimitedTextParser.Parse("\n,,\n", ',')));
        }
    }
}
