using System.Text.RegularExpressions;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>Turns a raw localized string into the text that will actually be rendered.</summary>
    public static class TextSanitizer
    {
        // {0}, {name}, {0:N2}, {player.name}. Placeholders with nested braces or '|' (plural / select forms)
        // are left alone, because their literal parts are rendered.
        static readonly Regex SimplePlaceholder = new Regex(@"\{[A-Za-z0-9_.\-]+(?::[^{}|]*)?\}", RegexOptions.Compiled);

        public static string Prepare(string text, ScanOptions options)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (options.StripRichTextTags) text = RichTextStripper.Strip(text);
            if (options.StripPlaceholders) text = StripPlaceholders(text);
            return text;
        }

        public static string StripPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text ?? string.Empty;
            return SimplePlaceholder.Replace(text, string.Empty);
        }
    }
}
