using System;
using System.Text.RegularExpressions;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Turns the Google Sheets links people actually paste into a direct CSV/TSV download URL that needs no OAuth or
    /// API key. Works for sheets published to the web (File > Share > Publish to web) and for sheets shared as
    /// "Anyone with the link can view". Any other http(s) URL is returned unchanged.
    /// </summary>
    public static class SheetUrlNormalizer
    {
        static readonly Regex GidPattern = new Regex(@"[#?&]gid=(\d+)", RegexOptions.Compiled);
        static readonly Regex OutputPattern = new Regex(@"[?&]output=(csv|tsv)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex SpreadsheetIdPattern = new Regex(@"/spreadsheets/d/([A-Za-z0-9_-]+)", RegexOptions.Compiled);

        public static bool TryNormalize(string input, out string url, out string error)
        {
            url = null;
            error = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "The sheet URL is empty.";
                return false;
            }

            string trimmed = input.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "Not a valid http(s) URL.";
                return false;
            }

            bool isGoogleSheet = uri.Host.Equals("docs.google.com", StringComparison.OrdinalIgnoreCase) &&
                                 uri.AbsolutePath.StartsWith("/spreadsheets/", StringComparison.Ordinal);
            if (!isGoogleSheet)
            {
                url = trimmed;
                return true;
            }

            Match gidMatch = GidPattern.Match(trimmed);
            string gid = gidMatch.Success ? gidMatch.Groups[1].Value : null;

            const string publishedPrefix = "/spreadsheets/d/e/";
            if (uri.AbsolutePath.StartsWith(publishedPrefix, StringComparison.Ordinal))
            {
                int pub = uri.AbsolutePath.IndexOf("/pub", publishedPrefix.Length, StringComparison.Ordinal);
                if (pub < 0)
                {
                    error = "This looks like a published sheet link but has no '/pub' part. Copy the link from File > Share > Publish to web.";
                    return false;
                }

                string path = uri.AbsolutePath.Substring(0, pub + "/pub".Length);
                Match outputMatch = OutputPattern.Match(trimmed);
                string output = outputMatch.Success ? outputMatch.Groups[1].Value.ToLowerInvariant() : "csv";
                string sheet = gid != null ? "gid=" + gid + "&single=true&" : string.Empty;
                url = "https://docs.google.com" + path + "?" + sheet + "output=" + output;
                return true;
            }

            Match idMatch = SpreadsheetIdPattern.Match(uri.AbsolutePath);
            if (!idMatch.Success)
            {
                error = "Could not find the spreadsheet id in the URL.";
                return false;
            }

            url = "https://docs.google.com/spreadsheets/d/" + idMatch.Groups[1].Value + "/export?format=csv&gid=" + (gid ?? "0");
            return true;
        }
    }
}
