using System;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Turns a finished download into either "ok" (null) or an error message that says what to do about it.
    /// Pure logic so it can be unit tested without network access.
    /// </summary>
    public static class DownloadDiagnosis
    {
        const string SharingHelp =
            "In Google Sheets, set Share > General access to 'Anyone with the link', or use File > Share > " +
            "Publish to web (CSV), and check that the link points to the right spreadsheet and tab.";

        /// <param name="transportOk">False when the request never got an HTTP response (offline, DNS, timeout).</param>
        public static string Describe(string url, bool transportOk, long responseCode, string contentType, string transportError)
        {
            bool isGoogleSheet = !string.IsNullOrEmpty(url) &&
                                 url.IndexOf("docs.google.com/spreadsheets", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!transportOk)
            {
                return "Download failed: " + (string.IsNullOrEmpty(transportError) ? "no response" : transportError) +
                       ". Check your internet connection and the URL.";
            }

            if (responseCode >= 400)
            {
                if (isGoogleSheet && (responseCode == 401 || responseCode == 403 || responseCode == 404))
                {
                    return "Google Sheets returned HTTP " + responseCode + ": the sheet is private, does not exist, or the " +
                           "link is incomplete. " + SharingHelp;
                }
                if (isGoogleSheet && responseCode == 400)
                {
                    return "Google Sheets returned HTTP 400. This usually means the tab in the link (gid=...) no longer " +
                           "exists. Open the tab you want to scan in Google Sheets and copy the link again.";
                }
                return "Download failed: HTTP " + responseCode + ".";
            }

            if (IsHtml(contentType))
            {
                return isGoogleSheet
                    ? "Google returned a web page (usually a sign-in page) instead of CSV. " + SharingHelp
                    : "The URL returned a web page instead of CSV/TSV. Use a direct link to the .csv or .tsv file.";
            }

            return null;
        }

        static bool IsHtml(string contentType) =>
            !string.IsNullOrEmpty(contentType) && contentType.IndexOf("text/html", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
