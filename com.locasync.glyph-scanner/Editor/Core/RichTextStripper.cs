using System;
using System.Collections.Generic;
using System.Text;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>A stretch of rendered text and the font tag it sits in.</summary>
    public readonly struct TextRun
    {
        public readonly string Text;

        /// <summary>
        /// The name from an enclosing &lt;font="name"&gt; tag, or null when the text is rendered with the text
        /// object's own font.
        /// </summary>
        public readonly string FontName;

        public TextRun(string text, string fontName)
        {
            Text = text;
            FontName = fontName;
        }
    }

    /// <summary>
    /// Turns text with TextMeshPro rich text tags into the characters that are actually rendered.
    /// Tags TMP recognises are removed; anything else that merely looks like a tag (for example "a &lt; b &gt; c")
    /// is kept, because TMP renders it literally. Text inside &lt;noparse&gt; is kept verbatim.
    /// Two kinds of tag change which glyphs are needed, so they are applied instead of just dropped:
    /// &lt;allcaps&gt;, &lt;uppercase&gt;, &lt;lowercase&gt; and &lt;smallcaps&gt; change the case of the letters, and
    /// &lt;font="name"&gt; renders the text inside it with another font asset.
    /// </summary>
    public static class RichTextStripper
    {
        const string NoParseClose = "</noparse>";
        const string DefaultFontName = "default";

        static readonly HashSet<string> KnownTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "action", "align", "allcaps", "alpha", "b", "br", "color", "cr", "cspace", "font", "font-weight",
            "gradient", "i", "indent", "line-height", "line-indent", "link", "lowercase", "margin", "margin-left",
            "margin-right", "mark", "material", "mspace", "nbsp", "nobr", "noparse", "page", "pos", "rotate", "s",
            "shy", "size", "smallcaps", "space", "sprite", "strikethrough", "style", "sub", "sup", "u", "uppercase",
            "voffset", "width", "zwj", "zwsp"
        };

        /// <summary>The rendered characters of all runs, in order.</summary>
        public static string Strip(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0) return text ?? string.Empty;

            List<TextRun> runs = Parse(text);
            if (runs.Count == 1) return runs[0].Text;

            var sb = new StringBuilder(text.Length);
            foreach (TextRun run in runs) sb.Append(run.Text);
            return sb.ToString();
        }

        /// <summary>Splits the text into runs of rendered characters, one run per stretch that uses the same font.</summary>
        public static List<TextRun> Parse(string text)
        {
            var runs = new List<TextRun>();
            if (string.IsNullOrEmpty(text)) return runs;
            if (text.IndexOf('<') < 0)
            {
                runs.Add(new TextRun(text, null));
                return runs;
            }

            var sb = new StringBuilder(text.Length);
            var fonts = new List<string>(); // enclosing <font> tags, innermost last; null means the object's own font
            int upper = 0, lower = 0, small = 0;

            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<' && TryReadTag(text, i, out int end, out string name, out bool closing))
                {
                    if (!closing && name == "noparse")
                    {
                        int close = text.IndexOf(NoParseClose, end + 1, StringComparison.OrdinalIgnoreCase);
                        int contentEnd = close < 0 ? text.Length : close;
                        for (int k = end + 1; k < contentEnd; k++) sb.Append(ApplyCase(text[k], upper, lower, small));
                        i = close < 0 ? text.Length : close + NoParseClose.Length;
                        continue;
                    }

                    switch (name)
                    {
                        case "font":
                            Flush(runs, sb, fonts);
                            if (!closing) fonts.Add(ReadFontName(text, i, end));
                            else if (fonts.Count > 0) fonts.RemoveAt(fonts.Count - 1);
                            break;
                        case "allcaps":
                        case "uppercase":
                            upper = Step(upper, closing);
                            break;
                        case "lowercase":
                            lower = Step(lower, closing);
                            break;
                        case "smallcaps":
                            small = Step(small, closing);
                            break;
                    }

                    i = end + 1;
                    continue;
                }

                sb.Append(ApplyCase(text[i], upper, lower, small));
                i++;
            }

            Flush(runs, sb, fonts);
            return runs;
        }

        static void Flush(List<TextRun> runs, StringBuilder sb, List<string> fonts)
        {
            if (sb.Length == 0) return;
            runs.Add(new TextRun(sb.ToString(), fonts.Count > 0 ? fonts[fonts.Count - 1] : null));
            sb.Clear();
        }

        static int Step(int depth, bool closing) => closing ? Math.Max(0, depth - 1) : depth + 1;

        // Same rules and precedence as TextMeshPro: uppercase wins over lowercase, which wins over small caps.
        static char ApplyCase(char c, int upper, int lower, int small)
        {
            if (upper > 0) return char.IsLower(c) ? char.ToUpperInvariant(c) : c;
            if (lower > 0) return char.IsUpper(c) ? char.ToLowerInvariant(c) : c;
            if (small > 0) return char.IsLower(c) ? char.ToUpperInvariant(c) : c;
            return c;
        }

        // <font="Name">, <font=Name> or <font="Name" material="...">. "default" switches back to the object's own font.
        static string ReadFontName(string text, int tagStart, int tagEnd)
        {
            int equals = text.IndexOf('=', tagStart, tagEnd - tagStart);
            if (equals < 0) return null;

            int from = equals + 1;
            int to;
            if (from < tagEnd && text[from] == '"')
            {
                from++;
                to = text.IndexOf('"', from, tagEnd - from);
                if (to < 0) to = tagEnd;
            }
            else
            {
                to = text.IndexOf(' ', from, tagEnd - from);
                if (to < 0) to = tagEnd;
            }

            string name = text.Substring(from, to - from).Trim();
            if (name.Length == 0 || name.Equals(DefaultFontName, StringComparison.OrdinalIgnoreCase)) return null;
            return name;
        }

        static bool TryReadTag(string text, int start, out int end, out string name, out bool closing)
        {
            name = null;
            closing = false;
            end = text.IndexOf('>', start + 1);
            if (end < 0) return false;

            int p = start + 1;
            if (p < end && text[p] == '/')
            {
                closing = true;
                p++;
            }

            // Colour shorthand: <#RGB>, <#RGBA>, <#RRGGBB>, <#RRGGBBAA>
            if (p < end && text[p] == '#')
            {
                name = "#";
                return !closing && IsHexColor(text, p + 1, end);
            }

            int nameStart = p;
            while (p < end && (char.IsLetterOrDigit(text[p]) || text[p] == '-')) p++;
            if (p == nameStart) return false;

            name = text.Substring(nameStart, p - nameStart).ToLowerInvariant();
            if (!KnownTags.Contains(name)) return false;
            if (p == end) return true;      // <b>, </b>
            if (closing) return false;      // closing tags take no attributes

            char next = text[p];
            return next == '=' || next == ' ';
        }

        static bool IsHexColor(string text, int from, int to)
        {
            int length = to - from;
            if (length != 3 && length != 4 && length != 6 && length != 8) return false;
            for (int i = from; i < to; i++)
            {
                if (!Uri.IsHexDigit(text[i])) return false;
            }
            return true;
        }
    }
}
