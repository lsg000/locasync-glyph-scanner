using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>Turns a raw localized string into the text that will actually be rendered.</summary>
    public static class TextSanitizer
    {
        // {0}, {name}, {0:N2}, {player.name}. Placeholders with nested braces or '|' (plural / select forms)
        // are left alone, because their literal parts are rendered.
        static readonly Regex SimplePlaceholder = new Regex(@"\{[A-Za-z0-9_.\-]+(?::[^{}|]*)?\}", RegexOptions.Compiled);

        /// <summary>The rendered text as one string, ignoring which font each part uses.</summary>
        public static string Prepare(string text, ScanOptions options)
        {
            List<TextRun> runs = PrepareRuns(text, options);
            if (runs.Count == 0) return string.Empty;
            if (runs.Count == 1) return runs[0].Text;

            var sb = new StringBuilder();
            foreach (TextRun run in runs) sb.Append(run.Text);
            return sb.ToString();
        }

        /// <summary>The rendered text, split where a &lt;font&gt; tag switches to another font asset.</summary>
        public static List<TextRun> PrepareRuns(string text, ScanOptions options)
        {
            var result = new List<TextRun>();
            if (string.IsNullOrEmpty(text)) return result;

            List<TextRun> runs;
            if (options.StripRichTextTags)
            {
                runs = RichTextStripper.Parse(text);
            }
            else
            {
                runs = new List<TextRun> { new TextRun(text, null) };
            }

            foreach (TextRun run in runs)
            {
                string rendered = run.Text;
                if (options.StripPlaceholders) rendered = StripPlaceholders(rendered);
                if (options.IncludeUppercaseForms) rendered += rendered.ToUpperInvariant();
                if (rendered.Length > 0) result.Add(new TextRun(rendered, run.FontName));
            }
            return result;
        }

        public static string StripPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text ?? string.Empty;
            return SimplePlaceholder.Replace(text, string.Empty);
        }
    }
}
