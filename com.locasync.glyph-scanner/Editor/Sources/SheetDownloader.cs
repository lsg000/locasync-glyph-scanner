using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Downloads text in the editor without entering Play Mode. Polls from EditorApplication.update so it behaves the
    /// same on Unity 2022.3 and Unity 6 and needs no async/await support.
    /// </summary>
    internal static class SheetDownloader
    {
        const int TimeoutSeconds = 30;

        public static void Download(string url, Action<string> onSuccess, Action<string> onError)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = TimeoutSeconds;
            request.SendWebRequest();

            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if (!request.isDone) return;
                EditorApplication.update -= poll;

                string text = null;
                string error = null;
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        error = "Download failed: " + request.error;
                    }
                    else if (IsHtml(request.GetResponseHeader("Content-Type")))
                    {
                        error = "The URL returned a web page instead of CSV/TSV. In Google Sheets use File > Share > " +
                                "Publish to web (CSV), or set General access to 'Anyone with the link'.";
                    }
                    else
                    {
                        text = request.downloadHandler.text;
                    }
                }
                finally
                {
                    request.Dispose();
                }

                try
                {
                    if (error != null) onError(error);
                    else onSuccess(text);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            };
            EditorApplication.update += poll;
        }

        static bool IsHtml(string contentType) =>
            !string.IsNullOrEmpty(contentType) && contentType.IndexOf("text/html", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
