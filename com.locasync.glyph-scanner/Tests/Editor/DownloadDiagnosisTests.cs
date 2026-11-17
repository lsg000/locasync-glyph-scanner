using NUnit.Framework;

namespace LocaSync.Editor.GlyphScanner.Tests
{
    public class DownloadDiagnosisTests
    {
        const string SheetUrl = "https://docs.google.com/spreadsheets/d/abc/export?format=csv&gid=0";
        const string OtherUrl = "https://example.com/strings.csv";

        [Test]
        public void Csv_response_is_ok()
        {
            Assert.IsNull(DownloadDiagnosis.Describe(SheetUrl, true, 200, "text/csv; charset=utf-8", null));
            Assert.IsNull(DownloadDiagnosis.Describe(OtherUrl, true, 200, null, null));
        }

        [TestCase(401)]
        [TestCase(403)]
        [TestCase(404)]
        public void Google_access_errors_explain_sharing(long code)
        {
            string message = DownloadDiagnosis.Describe(SheetUrl, true, code, "text/html; charset=utf-8", "HTTP/1.1 " + code);

            StringAssert.Contains("HTTP " + code, message);
            StringAssert.Contains("Anyone with the link", message);
        }

        [Test]
        public void Google_sign_in_page_explains_sharing()
        {
            string message = DownloadDiagnosis.Describe(SheetUrl, true, 200, "text/html; charset=utf-8", null);

            StringAssert.Contains("sign-in", message);
            StringAssert.Contains("Publish to web", message);
        }

        [Test]
        public void Other_html_pages_ask_for_a_direct_link()
        {
            StringAssert.Contains("direct link", DownloadDiagnosis.Describe(OtherUrl, true, 200, "text/html", null));
        }

        [Test]
        public void Other_http_errors_report_the_code()
        {
            Assert.AreEqual("Download failed: HTTP 500.", DownloadDiagnosis.Describe(OtherUrl, true, 500, "text/plain", null));
        }

        [Test]
        public void Network_failures_suggest_checking_the_connection()
        {
            string message = DownloadDiagnosis.Describe(SheetUrl, false, 0, null, "Cannot resolve destination host");

            StringAssert.Contains("Cannot resolve destination host", message);
            StringAssert.Contains("internet connection", message);
        }
    }
}
