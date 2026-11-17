using System;
using System.Collections.Generic;
using System.Text;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Removes TextMeshPro rich text tags so that only characters which will actually be rendered remain.
    /// Only tags TMP recognises are removed; anything else that merely looks like a tag (for example "a &lt; b &gt; c")
    /// is kept, because TMP renders it literally. Text inside &lt;noparse&gt; is kept verbatim.
    /// </summary>
    public static class RichTextStripper
    {
        const string NoParseClose = "</noparse>";

        static readonly HashSet<string> KnownTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "action", "align", "allcaps", "alpha", "b", "br", "color", "cr", "cspace", "font", "font-weight",
            "gradient", "i", "indent", "line-height", "line-indent", "link", "lowercase", "margin", "margin-left",
            "margin-right", "mark", "material", "mspace", "nbsp", "nobr", "noparse", "page", "pos", "rotate", "s",
            "shy", "size", "smallcaps", "space", "sprite", "strikethrough", "style", "sub", "sup", "u", "uppercase",
            "voffset", "width", "zwj", "zwsp"
        };

        public static string Strip(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0) return text ?? string.Empty;

            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '<' && TryReadTag(text, i, out int end, out string name, out bool closing))
                {
                    if (!closing && name == "noparse")
                    {
                        int close = text.IndexOf(NoParseClose, end + 1, StringComparison.OrdinalIgnoreCase);
                        int contentEnd = close < 0 ? text.Length : close;
                        sb.Append(text, end + 1, contentEnd - end - 1);
                        i = close < 0 ? text.Length : close + NoParseClose.Length;
                        continue;
                    }
                    i = end + 1;
                    continue;
                }

                sb.Append(text[i]);
                i++;
            }
            return sb.ToString();
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
