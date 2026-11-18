using System.Collections.Generic;
using System.Text;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// RFC 4180 style parser for comma, tab or semicolon separated text.
    /// Handles quoted fields, escaped quotes (""), line breaks inside quotes, CRLF/LF and a UTF-8 BOM.
    /// </summary>
    public static class DelimitedTextParser
    {
        static readonly char[] Candidates = { '\t', ',', ';' };

        /// <summary>Picks the delimiter that occurs most often in the first line (outside quotes). Defaults to comma.</summary>
        public static char DetectDelimiter(string text)
        {
            if (string.IsNullOrEmpty(text)) return ',';

            var counts = new int[Candidates.Length];
            bool inQuotes = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (inQuotes) continue;
                if (c == '\n' || c == '\r') break;
                for (int k = 0; k < Candidates.Length; k++)
                {
                    if (c == Candidates[k]) counts[k]++;
                }
            }

            int best = 1; // comma wins ties
            for (int k = 0; k < Candidates.Length; k++)
            {
                if (counts[k] > counts[best]) best = k;
            }
            return Candidates[best];
        }

        public static List<string[]> Parse(string text, char delimiter)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return rows;

            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int i = text[0] == '\uFEFF' ? 1 : 0;

            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else if (c == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row.ToArray());
                    row.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }
            return rows;
        }
    }
}
