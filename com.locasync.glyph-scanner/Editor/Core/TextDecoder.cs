using System;
using System.Text;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Decodes a localization file strictly, so a file saved in a legacy encoding is reported instead of being
    /// scanned as garbage. Accepts UTF-8 (with or without a byte order mark) and UTF-16 with a byte order mark.
    /// </summary>
    public static class TextDecoder
    {
        public const string WrongEncodingMessage =
            "The text is not valid UTF-8, so its characters cannot be read correctly. Excel's plain \"CSV\" format " +
            "saves in a legacy encoding such as CP949 or Shift-JIS. Save the file as \"CSV UTF-8\" (or export it " +
            "as UTF-8) and scan again.";

        const char ReplacementCharacter = '\uFFFD';

        /// <summary>Returns false when the bytes are not valid UTF-8 or BOM-marked UTF-16. The BOM is removed.</summary>
        public static bool TryDecode(byte[] bytes, out string text)
        {
            text = string.Empty;
            if (bytes == null || bytes.Length == 0) return true;

            Encoding encoding;
            int offset;
            if (StartsWith(bytes, 0xEF, 0xBB, 0xBF))
            {
                encoding = new UTF8Encoding(false, true);
                offset = 3;
            }
            else if (StartsWith(bytes, 0xFF, 0xFE))
            {
                encoding = new UnicodeEncoding(false, false, true);
                offset = 2;
            }
            else if (StartsWith(bytes, 0xFE, 0xFF))
            {
                encoding = new UnicodeEncoding(true, false, true);
                offset = 2;
            }
            else
            {
                encoding = new UTF8Encoding(false, true);
                offset = 0;
            }

            try
            {
                text = encoding.GetString(bytes, offset, bytes.Length - offset);
                return true;
            }
            catch (ArgumentException) // DecoderFallbackException: an invalid byte sequence
            {
                text = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// True when the text contains U+FFFD, the character decoders substitute for bytes they could not read.
        /// It means the text was decoded with the wrong encoding somewhere before it reached the scanner.
        /// </summary>
        public static bool LooksWronglyDecoded(string text) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(ReplacementCharacter) >= 0;

        static bool StartsWith(byte[] bytes, params byte[] prefix)
        {
            if (bytes.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (bytes[i] != prefix[i]) return false;
            }
            return true;
        }
    }
}
