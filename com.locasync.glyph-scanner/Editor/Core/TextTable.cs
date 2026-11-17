using System;
using System.Collections.Generic;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>A header row plus data rows, every data row padded to the header width. Blank rows are dropped.</summary>
    public sealed class TextTable
    {
        readonly List<string> _headers;
        readonly List<string[]> _rows;

        TextTable(List<string> headers, List<string[]> rows)
        {
            _headers = headers;
            _rows = rows;
        }

        public IReadOnlyList<string> Headers => _headers;
        public IReadOnlyList<string[]> Rows => _rows;

        /// <summary>Returns null when there is no non-blank row to use as the header.</summary>
        public static TextTable FromRows(IReadOnlyList<string[]> rows)
        {
            List<string> headers = null;
            var data = new List<string[]>();

            foreach (string[] row in rows)
            {
                if (row == null || IsBlank(row)) continue;

                if (headers == null)
                {
                    headers = new List<string>(row.Length);
                    foreach (string cell in row) headers.Add(cell.Trim());
                    continue;
                }

                var normalized = new string[headers.Count];
                for (int i = 0; i < normalized.Length; i++)
                {
                    normalized[i] = i < row.Length ? row[i] : string.Empty;
                }
                data.Add(normalized);
            }

            return headers == null ? null : new TextTable(headers, data);
        }

        /// <summary>Case-insensitive, whitespace-trimmed header lookup. Returns -1 when not found.</summary>
        public int IndexOf(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return -1;
            string wanted = header.Trim();
            for (int i = 0; i < _headers.Count; i++)
            {
                if (string.Equals(_headers[i], wanted, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        static bool IsBlank(string[] row)
        {
            foreach (string cell in row)
            {
                if (!string.IsNullOrWhiteSpace(cell)) return false;
            }
            return true;
        }
    }
}
